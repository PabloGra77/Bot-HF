using System.Diagnostics;
using HorusAfiliadosExtractor.App.Models;
using HorusAfiliadosExtractor.App.Utils;
using Microsoft.Playwright;

namespace HorusAfiliadosExtractor.App.Services;

public sealed class HorusExtractorBot
{
    private readonly BotSettings _cfg;
    private readonly Logger _log;
    private readonly LoginCredentials? _credentials;
    private readonly Action<int, int, string>? _onProgress;
    private readonly ExtractionControl? _control;

    public HorusExtractorBot(
        BotSettings cfg,
        Logger log,
        LoginCredentials? credentials = null,
        Action<int, int, string>? onProgress = null,
        ExtractionControl? control = null)
    {
        _cfg = cfg;
        _log = log;
        _credentials = credentials;
        _onProgress = onProgress;
        _control = control;
    }

    public async Task<List<ExtractionResult>> RunAsync(List<InputRecord> records, CancellationToken cancellationToken = default)
    {
        var results = new List<ExtractionResult>();

        _log.Warn("MODO SOLO CONSULTA/EXTRACCIÓN: no presiona Guardar, Actualizar, Eliminar, Registrar ni Enviar.");
        _log.Info($"Documentos a procesar: {records.Count}");
        _log.Info($"Perfil persistente Edge/Chrome: {_cfg.ProfileDir}");
        _log.Info(_cfg.IncrementalExcelSave
            ? $"Guardado incremental activo: se actualizará el Excel cada {_cfg.SaveEveryRecords} documento(s)."
            : "Guardado incremental desactivado: el Excel se escribirá al final.");

        using var playwright = await Playwright.CreateAsync();

        var launchArgs = new[] {
            "--start-maximized",
            "--disable-features=Translate",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-default-apps",
            "--disable-background-networking",
            "--disable-component-update",
            "--disable-domain-reliability"
        };

        IBrowserContext context = await LaunchBrowserWithFallbackAsync(playwright, launchArgs);

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
        page.SetDefaultTimeout(_cfg.TimeoutSeconds * 1000);
        page.SetDefaultNavigationTimeout(_cfg.TimeoutSeconds * 1000);

        page.Dialog += async (_, dialog) =>
        {
            try
            {
                _log.Warn($"Diálogo JS detectado: {dialog.Message}. Se acepta para no bloquear extracción.");
                await dialog.AcceptAsync();
            }
            catch { /* no bloquear extracción por diálogos */ }
        };

        try
        {
            await PrepareSessionAsync(page, cancellationToken);

            for (var i = 0; i < records.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Si el usuario presionó Detener, guardar Excel YA y esperar hasta Continuar / Finalizar.
                if (_control is { IsPaused: true })
                {
                    _log.Info("Pausa solicitada. Guardando Excel con lo procesado hasta este momento...");
                    ForceSaveExcel(results);
                    _onProgress?.Invoke(i, records.Count, "Pausado - Excel guardado");
                    await _control.WaitIfPausedAsync(cancellationToken);
                    _log.Info("Reanudando extracción.");
                }

                var record = records[i];
                _onProgress?.Invoke(i, records.Count, $"Procesando {record.Documento}");

                var sw = Stopwatch.StartNew();
                var result = new ExtractionResult
                {
                    DocumentoConsultado = record.Documento,
                    FechaExtraccion = DateTime.Now
                };

                try
                {
                    _log.Info($"==== Documento {record.Documento} ====");
                    await EnsureModuleAsync(page);

                    if (_cfg.ClickNuevaConsultaBeforeEach)
                        await ClickNuevaConsultaIfPresentAsync(page);

                    await SearchAffiliateAsync(page, record.Documento);
                    await WaitResultAsync(page, record.Documento);

                    result.PageUrl = page.Url;
                    await ExtractCurrentAndTabsAsync(page, result);

                    if (_cfg.SaveScreenshots)
                    {
                        result.ScreenshotPath = await SaveScreenshotAsync(page, record.Documento, "final");
                    }

                    result.Success = true;
                    result.Message = $"OK. Campos: {result.Fields.Count}, Celdas tabla: {result.TableCells.Count}";
                    _log.Info(result.Message);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"ERROR: {ex.Message}";
                    _log.Error(ex, $"Falló documento {record.Documento}");
                    try
                    {
                        result.ScreenshotPath = await SaveScreenshotAsync(page, record.Documento, "error");
                    }
                    catch { /* no bloquear */ }
                }
                finally
                {
                    sw.Stop();
                    result.Seconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
                    results.Add(result);
                    SaveIncrementalExcel(results, results.Count);
                    _onProgress?.Invoke(i + 1, records.Count, $"Procesado {record.Documento}");
                }
            }
        }
        finally
        {
            await context.CloseAsync();
        }

        return results;
    }

    private async Task<IBrowserContext> LaunchBrowserWithFallbackAsync(IPlaywright playwright, string[] launchArgs)
    {
        BrowserTypeLaunchPersistentContextOptions BuildOpts(string? channel) => new()
        {
            Channel = string.IsNullOrWhiteSpace(channel) ? null : channel,
            Headless = _cfg.Headless,
            SlowMo = _cfg.SlowMoMilliseconds,
            Args = launchArgs,
            AcceptDownloads = false
        };

        // Intento 1: canal configurado (msedge por defecto).
        try
        {
            _log.Info($"Lanzando navegador (canal '{_cfg.BrowserChannel}'). Perfil: {_cfg.ProfileDir}");
            return await playwright.Chromium.LaunchPersistentContextAsync(_cfg.ProfileDir, BuildOpts(_cfg.BrowserChannel));
        }
        catch (Exception exEdge)
        {
            _log.Warn($"No se pudo lanzar el navegador con canal '{_cfg.BrowserChannel}': {exEdge.Message}");
            _log.Warn("Sugerencia: cierre todas las ventanas de Microsoft Edge antes de ejecutar el bot.");
        }

        // Intento 2: instalar/usar Chromium descargado por Playwright.
        try
        {
            _log.Info("Reintentando con Chromium descargado por Playwright. Si es la primera vez puede tardar varios minutos en descargar (~150 MB).");
            try
            {
                var installExit = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                if (installExit != 0) _log.Warn($"'playwright install chromium' devolvió código {installExit}.");
            }
            catch (Exception exInst)
            {
                _log.Warn($"No se pudo descargar Chromium automáticamente: {exInst.Message}");
            }

            return await playwright.Chromium.LaunchPersistentContextAsync(_cfg.ProfileDir, BuildOpts(null));
        }
        catch (Exception exChromium)
        {
            _log.Error(exChromium, "No se pudo lanzar Chromium tampoco.");
            throw new InvalidOperationException(
                "No se pudo iniciar el navegador.\n\n" +
                "Verifique:\n" +
                "  1) Cierre todas las ventanas de Microsoft Edge antes de ejecutar.\n" +
                "  2) Verifique que Microsoft Edge esté instalado y actualizado.\n" +
                "  3) Si el problema persiste, revise el log de esta sesión.\n\n" +
                $"Detalle técnico: {exChromium.Message}", exChromium);
        }
    }

    private void SaveIncrementalExcel(IReadOnlyList<ExtractionResult> results, int processedCount)
    {
        if (!_cfg.IncrementalExcelSave) return;

        var every = Math.Max(1, _cfg.SaveEveryRecords);
        if (processedCount % every != 0) return;

        try
        {
            ExcelWriter.Save(_cfg.OutputExcelPath, results);
            _log.Info($"Excel incremental actualizado ({processedCount} registro(s)): {_cfg.OutputExcelPath}");
        }
        catch (Exception ex)
        {
            _log.Warn($"No se pudo actualizar el Excel incremental. Cierre el archivo si está abierto. Detalle: {ex.Message}");
        }
    }

    private void ForceSaveExcel(IReadOnlyList<ExtractionResult> results)
    {
        try
        {
            ExcelWriter.Save(_cfg.OutputExcelPath, results);
            _log.Info($"Excel guardado bajo demanda ({results.Count} registro(s)): {_cfg.OutputExcelPath}");
        }
        catch (Exception ex)
        {
            _log.Warn($"No se pudo guardar el Excel bajo demanda. Cierre el archivo si está abierto. Detalle: {ex.Message}");
        }
    }

    private async Task PrepareSessionAsync(IPage page, CancellationToken cancellationToken)
    {
        _log.Info($"Abriendo login/base: {_cfg.LoginUrl}");
        await page.GotoAsync(_cfg.LoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await SafeNetworkIdleAsync(page, 8000);

        if (_credentials is { IsValid: true })
        {
            await AttemptAutoLoginAsync(page, _credentials, cancellationToken);
        }
        else if (_cfg.ManualLogin)
        {
            _log.Warn("Sin credenciales: inicia sesión manualmente en la ventana del navegador.");
        }

        if (_cfg.NavigateModuleAfterLogin)
            await EnsureModuleAsync(page);
    }

    private async Task AttemptAutoLoginAsync(IPage page, LoginCredentials credentials, CancellationToken cancellationToken)
    {
        var typeLabel = credentials.Type == UserType.Funcionario ? "Funcionario" : "Afiliado";
        _log.Info($"Login automático como '{typeLabel}' ({credentials.Email})");

        try
        {
            // 1) Abrir el combobox Vuetify "Seleccione el tipo usuario".
            await OpenUserTypeDropdownAsync(page);
            cancellationToken.ThrowIfCancellationRequested();

            // 2) Seleccionar la opción Afiliado / Funcionario en el dropdown.
            await PickUserTypeOptionAsync(page, typeLabel);
            cancellationToken.ThrowIfCancellationRequested();

            // 3) Esperar a que aparezcan los campos (Vuetify los inserta dinámicamente al elegir tipo).
            await page.WaitForTimeoutAsync(600);

            // 4) Email: el v-text-field de Vuetify usa label flotante, no <label for>.
            var emailLoc = await ResolveLoginFieldAsync(page, "Email", _cfg.EmailFieldSelectors, isPassword: false, cancellationToken);
            try { await emailLoc.ClickAsync(new LocatorClickOptions { Timeout = 5000 }); } catch { }
            await emailLoc.FillAsync(credentials.Email);
            _log.Info("Email completado.");

            // 5) Password.
            var passLoc = await ResolveLoginFieldAsync(page, "Password", _cfg.PasswordFieldSelectors, isPassword: true, cancellationToken);
            try { await passLoc.ClickAsync(new LocatorClickOptions { Timeout = 5000 }); } catch { }
            await passLoc.FillAsync(credentials.Password);
            _log.Info("Contraseña completada.");
            cancellationToken.ThrowIfCancellationRequested();

            // 5) Botón INICIAR SESIÓN.
            var submitLoc = page.Locator(_cfg.SubmitButtonSelectors).First;
            if (await submitLoc.CountAsync() > 0)
                await submitLoc.ClickAsync(new LocatorClickOptions { Timeout = _cfg.TimeoutSeconds * 1000 });
            else
                await passLoc.PressAsync("Enter");

            await SafeNetworkIdleAsync(page, 12000);
            await page.WaitForTimeoutAsync(800);

            // 6) Verificar resultado: si la URL sigue en la raíz y vemos el formulario de login, posible error.
            var body = await BodyTextAsync(page);
            if (body.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
                || body.Contains("inválid", StringComparison.OrdinalIgnoreCase)
                || body.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                || body.Contains("credenciales", StringComparison.OrdinalIgnoreCase) && body.Contains("error", StringComparison.OrdinalIgnoreCase)
                || body.Contains("no autorizado", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Credenciales rechazadas por el servidor.");
            }

            _log.Info("Login automático enviado.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.Warn($"Login automático no completó: {ex.Message}. Si la ventana de navegador sigue en la pantalla de login, complete manualmente y vuelva a presionar continuar.");
        }
    }

    private async Task<ILocator> ResolveLoginFieldAsync(IPage page, string label, string fallbackSelectors, bool isPassword, CancellationToken ct)
    {
        var timeoutMs = _cfg.TimeoutSeconds * 1000;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        async Task<ILocator?> TryAsync(Func<ILocator> make)
        {
            try
            {
                var loc = make();
                if (await loc.CountAsync() == 0) return null;
                if (await loc.IsVisibleAsync()) return loc;
            }
            catch { }
            return null;
        }

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            // 1) GetByLabel — funciona si Vuetify expone aria-labelledby correctamente.
            var hit = await TryAsync(() => page.GetByLabel(label, new PageGetByLabelOptions { Exact = false }).First);
            if (hit != null) return hit;

            // 2) Vuetify v-text-field: <div class="v-input"> contiene <div class="v-field__label"> con el texto + <input>.
            hit = await TryAsync(() => page.Locator($"div.v-input:has(.v-field__label:has-text(\"{label}\")) input:not([type=hidden])").First);
            if (hit != null) return hit;

            // 3) Vuetify alternativo con <label>.
            hit = await TryAsync(() => page.Locator($"div.v-input:has(label:has-text(\"{label}\")) input:not([type=hidden])").First);
            if (hit != null) return hit;

            // 4) Por type del input (cuando aplica).
            if (isPassword)
            {
                hit = await TryAsync(() => page.Locator("input[type='password']:not([type=hidden])").First);
                if (hit != null) return hit;
            }
            else
            {
                hit = await TryAsync(() => page.Locator("input[type='email']:not([type=hidden])").First);
                if (hit != null) return hit;
            }

            // 5) Por aria-label o placeholder.
            hit = await TryAsync(() => page.Locator($"input[aria-label*='{label}' i]").First);
            if (hit != null) return hit;
            hit = await TryAsync(() => page.Locator($"input[placeholder*='{label}' i]").First);
            if (hit != null) return hit;

            // 6) Selectores configurados en appsettings.json.
            if (!string.IsNullOrWhiteSpace(fallbackSelectors))
            {
                hit = await TryAsync(() => page.Locator(fallbackSelectors).First);
                if (hit != null) return hit;
            }

            await page.WaitForTimeoutAsync(400);
        }

        throw new TimeoutException($"No se encontró el campo '{label}' visible en el formulario de login después de {timeoutMs / 1000}s.");
    }

    private async Task OpenUserTypeDropdownAsync(IPage page)
    {
        // Estrategias en orden de preferencia para abrir el v-select de tipo de usuario.
        var attempts = new Func<Task<bool>>[]
        {
            async () =>
            {
                var loc = page.GetByText("Seleccione el tipo usuario").First;
                if (await loc.CountAsync() == 0) return false;
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                var loc = page.Locator("[role='combobox']").First;
                if (await loc.CountAsync() == 0) return false;
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                var loc = page.Locator(".v-select, .v-autocomplete").First;
                if (await loc.CountAsync() == 0) return false;
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                var loc = page.Locator(".v-field, .v-input__control").First;
                if (await loc.CountAsync() == 0) return false;
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            }
        };

        foreach (var attempt in attempts)
        {
            try
            {
                if (await attempt())
                {
                    await page.WaitForTimeoutAsync(350);
                    _log.Info("Combobox de tipo de usuario abierto.");
                    return;
                }
            }
            catch { /* probar siguiente */ }
        }

        throw new InvalidOperationException("No se encontró el combobox 'Seleccione el tipo usuario'.");
    }

    private async Task PickUserTypeOptionAsync(IPage page, string optionText)
    {
        var attempts = new Func<Task<bool>>[]
        {
            async () =>
            {
                var loc = page.GetByRole(AriaRole.Option, new PageGetByRoleOptions { Name = optionText, Exact = true }).First;
                if (await loc.CountAsync() == 0) return false;
                await loc.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                var loc = page.Locator(".v-list-item").GetByText(optionText, new LocatorGetByTextOptions { Exact = true }).First;
                if (await loc.CountAsync() == 0) return false;
                await loc.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                var loc = page.Locator($".v-list-item:has-text(\"{optionText}\")").First;
                if (await loc.CountAsync() == 0) return false;
                await loc.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                return true;
            },
            async () =>
            {
                // Última opción: clic por JS evaluando el texto exacto.
                return await page.EvaluateAsync<bool>(
                    """
(text) => {
    const items = Array.from(document.querySelectorAll('.v-list-item, [role=option]'));
    const target = items.find(el => (el.innerText || '').trim().toUpperCase() === text.toUpperCase());
    if (!target) return false;
    target.scrollIntoView({block:'center'});
    target.click();
    return true;
}
""", optionText);
            }
        };

        foreach (var attempt in attempts)
        {
            try
            {
                if (await attempt())
                {
                    _log.Info($"Tipo de usuario seleccionado: {optionText}");
                    return;
                }
            }
            catch { /* probar siguiente */ }
        }

        throw new InvalidOperationException($"No se pudo seleccionar la opción '{optionText}' del combobox.");
    }

    private async Task EnsureModuleAsync(IPage page)
    {
        if (!page.Url.Contains("/aseguramiento/afiliados", StringComparison.OrdinalIgnoreCase))
        {
            _log.Info($"Navegando al módulo: {_cfg.ModuleUrl}");
            await page.GotoAsync(_cfg.ModuleUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await SafeNetworkIdleAsync(page, 10000);
        }

        // Heurística más estricta: solo grita login si la URL contiene 'login' o el header tiene formulario de login.
        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
            page.Url.Contains("/auth", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La sesión no está autenticada o el sistema devolvió al login.");
        }
    }

    private async Task ClickNuevaConsultaIfPresentAsync(IPage page)
    {
        var loc = page.Locator("button:has-text('NUEVA CONSULTA'), .v-btn:has-text('NUEVA CONSULTA')").First;
        if (await loc.CountAsync() == 0) return;

        try
        {
            _log.Info("Limpiando pantalla con NUEVA CONSULTA; esto solo reinicia la vista, no modifica datos guardados.");
            await SafeClickAllowedAsync(loc, "NUEVA CONSULTA", 2500);
            await page.WaitForTimeoutAsync(700);
        }
        catch
        {
            // Si no se puede limpiar, se llenará directamente el documento.
        }
    }

    private async Task SearchAffiliateAsync(IPage page, string documento)
    {
        _log.Info($"Consultando afiliado: {documento}");

        var input = page.GetByLabel("Documento del afiliado").First;
        if (await input.CountAsync() == 0)
            input = page.Locator("input[type='text']").First;

        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = _cfg.TimeoutSeconds * 1000 });
        await input.ClickAsync(new LocatorClickOptions { Timeout = _cfg.TimeoutSeconds * 1000 });
        await input.FillAsync(documento);
        await page.WaitForTimeoutAsync(300);

        var btn = page.Locator("button:has-text('CONSULTAR AFILIADO'), .v-btn:has-text('CONSULTAR AFILIADO')").First;
        await SafeClickAllowedAsync(btn, "CONSULTAR AFILIADO", _cfg.TimeoutSeconds * 1000);
    }

    private async Task WaitResultAsync(IPage page, string documento)
    {
        await page.WaitForTimeoutAsync(_cfg.WaitAfterSearchMilliseconds);
        await SafeNetworkIdleAsync(page, 10000);

        try
        {
            await page.WaitForFunctionAsync(
                "(doc) => { const t = document.body.innerText || ''; return t.includes(doc) && (t.includes('DATOS BÁSICOS') || t.includes('DATOS BASICOS') || t.includes('Estado') || t.includes('ACTIVO') || t.includes('BENEFICIARIO') || t.includes('COTIZANTE')); }",
                documento,
                new PageWaitForFunctionOptions { Timeout = _cfg.TimeoutSeconds * 1000 });
        }
        catch
        {
            var body = await BodyTextAsync(page);
            if (body.Contains("Ningún", StringComparison.OrdinalIgnoreCase) || body.Contains("No se", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"No se encontraron datos para el documento {documento}.");

            throw new TimeoutException($"No se confirmó carga del afiliado {documento}. Revise si la consulta devolvió datos o si apareció un mensaje en pantalla.");
        }
    }

    private async Task ExtractCurrentAndTabsAsync(IPage page, ExtractionResult result)
    {
        List<string> tabs;
        try
        {
            tabs = await DiscoverTabsAsync(page);
        }
        catch (Exception ex)
        {
            _log.Warn($"No se pudieron descubrir pestañas por DOM. Se usará la lista configurada. Detalle: {ex.Message}");
            tabs = new List<string>();
        }

        if (!tabs.Any(t => SameNorm(t, "VISTA ACTUAL")))
            tabs.Insert(0, "VISTA ACTUAL");

        foreach (var configured in _cfg.TabsToExtract)
        {
            if (!tabs.Any(t => SameNorm(t, configured)))
                tabs.Add(configured);
        }

        foreach (var tab in tabs)
        {
            try
            {
                if (!SameNorm(tab, "VISTA ACTUAL"))
                {
                    var clicked = await ClickTabByTextAsync(page, tab);
                    if (!clicked)
                    {
                        _log.Warn($"No se pudo abrir la pestaña '{tab}'. Se omite.");
                        continue;
                    }
                    await page.WaitForTimeoutAsync(_cfg.WaitAfterTabClickMilliseconds);
                    await SafeNetworkIdleAsync(page, 5000);
                }

                var payload = await DomExtractor.ExtractAsync(page, tab, _cfg.ExtractVisibleBodyText);
                AppendPayload(result, payload);

                if (_cfg.SaveScreenshots)
                    await SaveScreenshotAsync(page, result.DocumentoConsultado, SanitizeFileName(tab));

                _log.Info($"Pestaña '{tab}': {payload.Fields.Count} campos, {payload.Tables.Count} tablas.");
            }
            catch (Exception ex)
            {
                _log.Warn($"No se pudo extraer pestaña '{tab}': {ex.Message}");
            }
        }
    }

    private void AppendPayload(ExtractionResult result, DomExtractionPayload payload)
    {
        foreach (var f in payload.Fields)
        {
            result.Fields.Add(new FieldValue
            {
                DocumentoConsultado = result.DocumentoConsultado,
                FechaExtraccion = result.FechaExtraccion,
                Tab = payload.TabName,
                Section = f.Section,
                Campo = f.Label,
                Valor = f.Value,
                Source = f.Source
            });
        }

        foreach (var table in payload.Tables)
        {
            for (var r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                for (var c = 0; c < row.Count; c++)
                {
                    var colName = c < table.Headers.Count && !string.IsNullOrWhiteSpace(table.Headers[c])
                        ? table.Headers[c]
                        : $"Columna_{c + 1}";

                    result.TableCells.Add(new TableCellValue
                    {
                        DocumentoConsultado = result.DocumentoConsultado,
                        FechaExtraccion = result.FechaExtraccion,
                        Tab = payload.TabName,
                        TableName = table.Name,
                        RowIndex = r + 1,
                        ColumnName = colName,
                        Valor = row[c]
                    });
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.BodyText))
        {
            result.BodyTexts.Add(new BodyTextValue
            {
                DocumentoConsultado = result.DocumentoConsultado,
                FechaExtraccion = result.FechaExtraccion,
                Tab = payload.TabName,
                Text = payload.BodyText
            });
        }
    }

    private async Task<List<string>> DiscoverTabsAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
() => {
    const clean = s => (s || '')
        .replace(/​/g, ' ')
        .replace(/ /g, ' ')
        .replace(/\s+/g, ' ')
        .trim();

    const visible = el => {
        if (!el) return false;
        const r = el.getBoundingClientRect();
        const s = getComputedStyle(el);
        return r.width > 0 && r.height > 0 &&
               s.display !== 'none' &&
               s.visibility !== 'hidden' &&
               s.opacity !== '0';
    };

    const items = Array.from(document.querySelectorAll('.v-tab,[role=tab],.v-tabs a,.v-tabs button'))
        .filter(visible)
        .map(el => clean(el.innerText || el.textContent || el.getAttribute('aria-label') || ''))
        .filter(x => x && x.length <= 80);

    return JSON.stringify(items);
}
""");

        List<string>? values;
        try { values = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json); }
        catch { values = new List<string>(); }

        return (values ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .DistinctBy(Normalize)
            .ToList();
    }

    private async Task<bool> ClickTabByTextAsync(IPage page, string tabText)
    {
        var ok = await page.EvaluateAsync<bool>(
            """
(name) => {
    const clean = s => (s || '').replace(/​/g,' ').replace(/ /g,' ').replace(/\s+/g,' ').trim();
    const norm = s => clean(s).normalize('NFD').replace(/[̀-ͯ]/g,'').toUpperCase();
    const target = norm(name);
    const candidates = Array.from(document.querySelectorAll('.v-tab,[role=tab],button,a'))
      .filter(el => norm(el.innerText || el.textContent || '').includes(target));
    const el = candidates[0];
    if (!el) return false;
    el.scrollIntoView({behavior:'instant', block:'center', inline:'center'});
    el.click();
    return true;
}
""",
            tabText);

        return ok;
    }

    private async Task SafeClickAllowedAsync(ILocator locator, string actionText, float timeout)
    {
        var forbidden = new[] { "GUARDAR", "ACTUALIZAR", "ELIMINAR", "BORRAR", "ANULAR", "RADICAR", "ENVIAR", "REGISTRAR", "CONFIRMAR" };
        var normalized = Normalize(actionText);
        if (forbidden.Any(x => normalized.Contains(x)))
            throw new InvalidOperationException($"Acción bloqueada por seguridad: {actionText}");

        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });
        await locator.ClickAsync(new LocatorClickOptions { Timeout = timeout });
    }

    private async Task SafeNetworkIdleAsync(IPage page, int timeoutMs)
    {
        try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = timeoutMs }); }
        catch { /* SPAs raramente entran en idle */ }
    }

    private async Task<string> BodyTextAsync(IPage page)
    {
        try { return await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 5000 }); }
        catch { return string.Empty; }
    }

    private async Task<string> SaveScreenshotAsync(IPage page, string documento, string suffix)
    {
        var dir = Path.Combine(_cfg.EvidenceDir, DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{SanitizeFileName(documento)}_{SanitizeFileName(suffix)}_{DateTime.Now:HHmmss}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        return path;
    }

    private static bool SameNorm(string a, string b) => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value)
    {
        var formD = (value ?? string.Empty).Trim().Normalize(System.Text.NormalizationForm.FormD);
        var chars = formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
    }
}
