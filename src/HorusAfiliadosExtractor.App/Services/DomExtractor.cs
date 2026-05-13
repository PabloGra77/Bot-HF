using System.Text.Json;
using HorusAfiliadosExtractor.App.Models;
using Microsoft.Playwright;

namespace HorusAfiliadosExtractor.App.Services;

public static class DomExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<DomExtractionPayload> ExtractAsync(IPage page, string tabName, bool includeBodyText)
    {
        var json = await page.EvaluateAsync<string>(ExtractionScript, new { tabName, includeBodyText });
        return JsonSerializer.Deserialize<DomExtractionPayload>(json, JsonOptions) ?? new DomExtractionPayload { TabName = tabName };
    }

    private const string ExtractionScript = """
(args) => {
    const tabName = args.tabName || 'ACTUAL';
    const includeBodyText = !!args.includeBodyText;
    const clean = (s) => (s || '')
        .replace(/\u200B/g, ' ')
        .replace(/\u00A0/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();

    const norm = (s) => clean(s)
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toUpperCase();

    const isVisible = (el) => {
        if (!el) return false;
        const style = window.getComputedStyle(el);
        if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') return false;
        const rect = el.getBoundingClientRect();
        return rect.width > 0 && rect.height > 0;
    };

    const result = {
        url: location.href,
        title: document.title || '',
        tabName,
        fields: [],
        tables: [],
        bodyText: ''
    };

    const seen = new Set();
    const pushField = (section, label, value, source) => {
        label = clean(label);
        value = clean(value);
        section = clean(section || tabName);
        if (!label || !value) return;
        if (label.length > 140 || value.length > 2500) return;
        const key = norm(section + '|' + label + '|' + value + '|' + source);
        if (seen.has(key)) return;
        seen.add(key);
        result.fields.push({ section, label, value, source: source || '' });
    };

    const main = document.querySelector('main') || document.querySelector('#app') || document.body;
    const activeTabRoot = document.querySelector('.v-window-item--active') || main;

    const removeLabelFromText = (text, label) => {
        text = clean(text);
        label = clean(label);
        if (!text || !label) return text;
        if (norm(text) === norm(label)) return '';
        const rx = new RegExp('^' + label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i');
        return clean(text.replace(rx, ''));
    };

    const extractInputBox = (box, section) => {
        if (!isVisible(box)) return;
        let label = clean(
            box.querySelector('label')?.innerText ||
            box.querySelector('.v-label')?.innerText ||
            box.getAttribute('aria-label') ||
            box.querySelector('[aria-label]')?.getAttribute('aria-label') ||
            ''
        );

        const values = [];
        box.querySelectorAll('input, textarea, select').forEach(el => {
            if (!isVisible(el)) return;
            let v = '';
            if (el.tagName.toLowerCase() === 'select') {
                v = clean(el.options?.[el.selectedIndex]?.text || el.value || '');
            } else {
                v = clean(el.value || el.getAttribute('value') || '');
            }
            if (v && !['on', 'off', 'true', 'false'].includes(v.toLowerCase())) values.push(v);
        });

        box.querySelectorAll('.v-select__selection, .v-chip__content, .v-select__selection--comma').forEach(el => {
            const v = clean(el.innerText);
            if (v) values.push(v);
        });

        if (!label) {
            const text = clean(box.innerText);
            const parts = text.split(' ');
            if (parts.length > 1) label = parts.slice(0, Math.min(4, parts.length - 1)).join(' ');
        }

        let value = clean([...new Set(values)].join(' | '));
        if (!value) {
            const text = clean(box.innerText);
            value = removeLabelFromText(text, label);
        }

        pushField(section, label, value, 'v-input');
    };

    // 1) Campos del formulario activo de la pestaña.
    activeTabRoot.querySelectorAll('.v-input, .v-text-field, .v-select, .v-textarea').forEach(box => {
        extractInputBox(box, tabName);
    });

    // 2) Campos/resumen superior del afiliado que no siempre aparecen como inputs.
    const summaryCandidates = Array.from(main.querySelectorAll('.v-card, .v-sheet, .v-alert, .col, .row'));
    summaryCandidates.forEach((el, index) => {
        if (!isVisible(el)) return;
        const txt = clean(el.innerText);
        if (!txt || txt.length < 4 || txt.length > 500) return;
        if (norm(txt).includes('ASEGURAMIENTOS') || norm(txt).includes('BUSCAR MODULO')) return;
        if (norm(txt).includes('DATOS BASICOS') && txt.length > 120) return;
        if (el.querySelector('input, textarea, select')) return;

        const lines = txt.split(/\n|\r/).map(clean).filter(Boolean);
        if (lines.length >= 2 && lines.length <= 8) {
            const label = lines[0];
            const value = lines.slice(1).join(' | ');
            pushField('RESUMEN SUPERIOR', label, value, 'summary-card');
        } else if (txt.includes(' • ')) {
            const parts = txt.split(' • ').map(clean).filter(Boolean);
            if (parts.length >= 2) pushField('RESUMEN SUPERIOR', parts[0], parts.slice(1).join(' | '), 'summary-card');
        }
    });

    // 3) Tablas HTML/Vuetify visibles.
    const tables = Array.from(activeTabRoot.querySelectorAll('table'));
    tables.forEach((table, tableIndex) => {
        if (!isVisible(table)) return;
        const headers = Array.from(table.querySelectorAll('thead th')).map(th => clean(th.innerText)).filter(Boolean);
        const rows = [];
        table.querySelectorAll('tbody tr').forEach(tr => {
            if (!isVisible(tr)) return;
            const cells = Array.from(tr.querySelectorAll('td')).map(td => clean(td.innerText));
            if (cells.some(c => c)) rows.push(cells);
        });
        if (rows.length > 0 || headers.length > 0) {
            result.tables.push({ name: `${tabName}_tabla_${tableIndex + 1}`, headers, rows });
        }
    });

    // 4) Si la tabla no es table real, capturar filas visibles de v-data-table.
    if (result.tables.length === 0) {
        activeTabRoot.querySelectorAll('.v-data-table').forEach((dt, tableIndex) => {
            if (!isVisible(dt)) return;
            const text = clean(dt.innerText);
            if (!text || text.length < 4) return;
            const lines = text.split(/\n|\r/).map(clean).filter(Boolean);
            if (lines.length === 0) return;
            result.tables.push({ name: `${tabName}_datatable_${tableIndex + 1}`, headers: ['Texto'], rows: lines.map(x => [x]) });
        });
    }

    if (includeBodyText) {
        let body = clean(activeTabRoot.innerText || '');
        if (body.length > 12000) body = body.substring(0, 12000);
        result.bodyText = body;
    }

    return JSON.stringify(result);
}
""";
}
