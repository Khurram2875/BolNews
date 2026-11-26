import { EditorView, keymap, lineNumbers, highlightActiveLine, highlightActiveLineGutter, drawSelection, placeholder } from "@codemirror/view";
import { EditorState } from "@codemirror/state";
import { defaultKeymap, history, historyKeymap, indentWithTab } from "@codemirror/commands";
import { css, cssLanguage } from "@codemirror/lang-css";
import { syntaxHighlighting, defaultHighlightStyle, bracketMatching, indentOnInput, syntaxTree } from "@codemirror/language";
import { autocompletion, closeBrackets, closeBracketsKeymap, completionKeymap } from "@codemirror/autocomplete";
import { linter, lintGutter, lintKeymap } from "@codemirror/lint";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";

// Rules the server also enforces (CustomCssValidator.cs). Keep the two lists in step.
const FORBIDDEN = [
  [/@import\b/i, "@import is not allowed. Put the rules directly in this editor."],
  [/javascript\s*:/i, "javascript: URLs are not allowed."],
  [/vbscript\s*:/i, "vbscript: URLs are not allowed."],
  [/expression\s*\(/i, "expression() is not allowed."],
  [/behavior\s*:/i, "behavior is not allowed."],
  [/-moz-binding/i, "-moz-binding is not allowed."],
  [/data\s*:\s*text\/html/i, "data:text/html URLs are not allowed."],
  [/<\/|<!--/, "HTML tags are not allowed in CSS (remove < characters)."]
];

function stripComments(text) {
  // Same length as the input so positions still line up.
  return text.replace(/\/\*[\s\S]*?\*\//g, m => m.replace(/[^\n]/g, " "));
}

function makeLinter(knownClasses) {
  const known = new Set(knownClasses || []);
  return linter(view => {
    const diagnostics = [];
    const doc = view.state.doc;
    const text = doc.toString();

    // 1) Syntax problems found by the CSS parser.
    syntaxTree(view.state).iterate({
      enter: node => {
        if (node.type.isError) {
          const from = Math.min(node.from, doc.length);
          const to = Math.min(Math.max(node.to, from + 1), doc.length);
          diagnostics.push({ from, to, severity: "error", message: "This does not look like valid CSS here (check for a missing ; : { or })." });
        }
      }
    });

    // 2) Blocked constructs.
    const plain = stripComments(text);
    for (const [re, message] of FORBIDDEN) {
      const g = new RegExp(re.source, re.flags.includes("g") ? re.flags : re.flags + "g");
      let m;
      while ((m = g.exec(plain)) !== null) {
        diagnostics.push({ from: m.index, to: m.index + Math.max(m[0].length, 1), severity: "error", message });
        if (m[0].length === 0) g.lastIndex++;
      }
    }

    // 3) Brace balance outside strings and comments.
    let depth = 0, inString = null, firstOpen = -1;
    for (let i = 0; i < plain.length; i++) {
      const c = plain[i];
      if (inString) { if (c === "\\") i++; else if (c === inString) inString = null; continue; }
      if (c === '"' || c === "'") { inString = c; continue; }
      if (c === "{") { if (depth === 0) firstOpen = i; depth++; }
      else if (c === "}") {
        depth--;
        if (depth < 0) { diagnostics.push({ from: i, to: i + 1, severity: "error", message: "Extra } with no matching {." }); depth = 0; }
      }
    }
    if (depth > 0) diagnostics.push({ from: firstOpen, to: firstOpen + 1, severity: "error", message: "This { is never closed with }." });

    // 4) Likely typos in site class names.
    if (known.size) {
      syntaxTree(view.state).iterate({
        enter: node => {
          if (node.name === "ClassName") {
            const name = text.slice(node.from, node.to);
            if (name.startsWith("bn-") && !known.has(name)) {
              diagnostics.push({ from: node.from, to: node.to, severity: "warning", message: "No site class called ." + name + ". Check the spelling (see the Classes tab)." });
            }
          }
        }
      });
    }
    return diagnostics;
  }, { delay: 350 });
}

function makeCompletions(classes, variables) {
  return context => {
    // .bn-… class names
    const cls = context.matchBefore(/\.[\w-]*/);
    if (cls && (cls.from !== cls.to || context.explicit)) {
      return {
        from: cls.from + 1,
        options: classes.map(c => ({ label: c.replace(/^\./, ""), type: "class" })),
        validFor: /^[\w-]*$/
      };
    }
    // --bn-… theme variables (inside var(...) or when declaring)
    const v = context.matchBefore(/--[\w-]*/);
    if (v && (v.from !== v.to || context.explicit)) {
      return {
        from: v.from,
        options: variables.map(x => ({ label: x.name, type: "variable", detail: x.hint || "" })),
        validFor: /^[\w-]*$/
      };
    }
    return null;
  };
}

const theme = EditorView.theme({
  "&": { fontSize: "13.5px", border: "1px solid var(--bs-border-color, #ced4da)", borderRadius: "6px", backgroundColor: "#fff", color: "#1b2430" },
  "&.cm-focused": { outline: "2px solid rgba(179,20,27,.45)", outlineOffset: "1px" },
  ".cm-scroller": { fontFamily: "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace", lineHeight: "1.55", minHeight: "320px", maxHeight: "70vh" },
  ".cm-gutters": { backgroundColor: "#f4f5f7", color: "#8a94a3", border: "none", borderRight: "1px solid #e3e6ea" },
  ".cm-activeLine": { backgroundColor: "rgba(179,20,27,.05)" },
  ".cm-activeLineGutter": { backgroundColor: "rgba(179,20,27,.08)" }
});

export function create(options) {
  const { parent, value, classes = [], variables = [], onChange, readOnly } = options;
  const extensions = [
    lineNumbers(), highlightActiveLineGutter(), highlightActiveLine(), drawSelection(),
    history(), indentOnInput(), bracketMatching(), closeBrackets(), highlightSelectionMatches(),
    syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
    css(),
    cssLanguage.data.of({ autocomplete: makeCompletions(classes, variables) }),
    autocompletion({ activateOnTyping: true }),
    makeLinter(classes.map(c => c.replace(/^\./, ""))),
    lintGutter(),
    placeholder(options.placeholder || ""),
    keymap.of([...closeBracketsKeymap, ...defaultKeymap, ...searchKeymap, ...historyKeymap, ...completionKeymap, ...lintKeymap, indentWithTab]),
    EditorView.lineWrapping,
    theme,
    EditorView.updateListener.of(u => { if (u.docChanged && onChange) onChange(u.state.doc.toString()); })
  ];
  if (readOnly) extensions.push(EditorState.readOnly.of(true));
  const view = new EditorView({ parent, state: EditorState.create({ doc: value || "", extensions }) });
  return {
    view,
    getValue: () => view.state.doc.toString(),
    focus: () => view.focus(),
    /** Inserts text at the cursor (or replaces the selection) and keeps focus in the editor. */
    insert(text) {
      const sel = view.state.selection.main;
      view.dispatch({ changes: { from: sel.from, to: sel.to, insert: text }, selection: { anchor: sel.from + text.length }, scrollIntoView: true });
      view.focus();
    },
    /** Appends a block at the end, separated by a blank line. */
    append(text) {
      const doc = view.state.doc;
      const prefix = doc.length === 0 ? "" : (doc.toString().endsWith("\n") ? "\n" : "\n\n");
      const insert = prefix + text.trimEnd() + "\n";
      view.dispatch({ changes: { from: doc.length, insert }, selection: { anchor: doc.length + insert.length }, scrollIntoView: true });
      view.focus();
    }
  };
}
