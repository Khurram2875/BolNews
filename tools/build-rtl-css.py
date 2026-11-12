#!/usr/bin/env python3
"""Generate right-to-left copies of the public stylesheets by mirroring every physical left/right declaration.

Usage:  python3 tools/build-rtl-css.py
Reads   BolNews.Web/wwwroot/css/site.css, pages/home.css, pages/live.css
Writes  the same names with .rtl.css next to them.  Re-run after editing the source CSS.
Bootstrap ships its own RTL build (bootstrap.rtl.min.css) and is not touched here.
"""
import re, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent / "BolNews.Web" / "wwwroot" / "css"
FILES = ["site.css", "pages/home.css", "pages/live.css"]

SWAP_PROP = {}
for a, b in [("left", "right")]:
    for base in ["margin", "padding", "border", "scroll-margin", "scroll-padding"]:
        SWAP_PROP[f"{base}-{a}"] = f"{base}-{b}"; SWAP_PROP[f"{base}-{b}"] = f"{base}-{a}"
    for suffix in ["color", "width", "style"]:
        SWAP_PROP[f"border-{a}-{suffix}"] = f"border-{b}-{suffix}"; SWAP_PROP[f"border-{b}-{suffix}"] = f"border-{a}-{suffix}"
    SWAP_PROP["border-top-left-radius"] = "border-top-right-radius"; SWAP_PROP["border-top-right-radius"] = "border-top-left-radius"
    SWAP_PROP["border-bottom-left-radius"] = "border-bottom-right-radius"; SWAP_PROP["border-bottom-right-radius"] = "border-bottom-left-radius"
    SWAP_PROP["left"] = "right"; SWAP_PROP["right"] = "left"

def split_top(value):
    """Split on whitespace outside parentheses."""
    out, depth, cur = [], 0, ""
    for ch in value:
        if ch == "(": depth += 1
        if ch == ")": depth -= 1
        if ch.isspace() and depth == 0:
            if cur: out.append(cur); cur = ""
        else: cur += ch
    if cur: out.append(cur)
    return out

def flip_four(tokens, radius=False):
    if len(tokens) != 4: return tokens
    if radius: return [tokens[1], tokens[0], tokens[3], tokens[2]]
    return [tokens[0], tokens[3], tokens[2], tokens[1]]

def negate(num):
    num = num.strip()
    if re.fullmatch(r"-?0*\.?0+(px|rem|em|%)?", num): return num
    return num[1:] if num.startswith("-") else "-" + num

def flip_value(prop, value):
    p = prop.lower(); v = value
    important = ""
    m = re.search(r"\s*!important\s*$", v)
    if m: important = " !important"; v = v[:m.start()]
    if p in ("margin", "padding", "border-width", "border-color", "border-style", "scroll-margin", "scroll-padding", "inset"):
        v = " ".join(flip_four(split_top(v)))
    elif p == "border-radius":
        if "/" not in v: v = " ".join(flip_four(split_top(v), radius=True))
    elif p in ("text-align", "float", "clear"):
        v = re.sub(r"\bleft\b", "\0", v); v = re.sub(r"\bright\b", "left", v); v = v.replace("\0", "right")
    elif p in ("background-position", "object-position", "transform-origin", "background", "mask-position"):
        v = re.sub(r"\bleft\b", "\0", v); v = re.sub(r"\bright\b", "left", v); v = v.replace("\0", "right")
        v = re.sub(r"\bto right\b", "to \0", v); v = re.sub(r"\bto left\b", "to right", v); v = v.replace("to \0", "to left")
    elif p in ("background-image",):
        v = re.sub(r"\bto right\b", "to \0", v); v = re.sub(r"\bto left\b", "to right", v); v = v.replace("to \0", "to left")
    if "gradient" in v and p not in ("background", "background-image"):
        v = re.sub(r"\bto right\b", "to \0", v); v = re.sub(r"\bto left\b", "to right", v); v = v.replace("to \0", "to left")
    if p in ("transform", "translate"):
        v = re.sub(r"translateX\(\s*([^)]+?)\s*\)", lambda m: f"translateX({negate(m.group(1))})", v)
        v = re.sub(r"translate\(\s*([^,)]+?)\s*(,[^)]*)?\)", lambda m: f"translate({negate(m.group(1))}{m.group(2) or ''})", v)
    if p in ("box-shadow", "text-shadow", "filter"):
        def shadow(seg):
            toks = split_top(seg.strip())
            if toks and re.fullmatch(r"-?[\d.]+(px|rem|em)?", toks[0]) and len(toks) > 1 and not toks[0].startswith("inset"):
                toks[0] = negate(toks[0])
            return " ".join(toks)
        if p != "filter": v = ",".join(shadow(s) for s in re.split(r",(?![^(]*\))", v))
    return v + important

DECL = re.compile(r"(?P<prop>(?<![\w-])[a-zA-Z-]+)(?P<sep>\s*:\s*)(?P<val>[^;{}]+?)(?P<end>\s*;|\s*(?=\}))")

def flip_css(css):
    def repl(m):
        prop = m.group("prop")
        newprop = SWAP_PROP.get(prop.lower(), prop)
        if prop.lower() == "font-family" and "var(" not in m.group("val") and not re.search(r"icon|awesome|glyph|symbol|monospace|mono", m.group("val"), re.I):
            # Hard-coded Latin faces (Inter, Poppins, Belleza) have no Urdu glyphs: use the site's Urdu font stack.
            imp = " !important" if "!important" in m.group("val") else ""
            return f"font-family{m.group('sep')}var(--bn-font-family){imp}{m.group('end')}"
        return f"{newprop}{m.group('sep')}{flip_value(prop, m.group('val'))}{m.group('end')}"
    # Only declarations inside braces; strip comments first so selectors are never matched.
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    out, depth, buf = [], 0, ""
    i = 0
    # Walk once: text between '{' and the next '{' or '}' is a declaration block.
    pieces = re.split(r"([{}])", css)
    result, stack, last_sel = [], [], ""
    for idx, piece in enumerate(pieces):
        if piece == "{": stack.append(idx); result.append(piece); continue
        if piece == "}": stack and stack.pop(); result.append(piece); continue
        nxt = pieces[idx + 1] if idx + 1 < len(pieces) else ""
        # a declaration block is a segment directly followed by '}' (leaf); segments followed by '{' are selectors/at-rules
        if nxt == "}":
            # Author-chosen alignment (Quill's .ql-align-left/right) is physical on purpose: never mirror it.
            result.append(piece if "ql-align-" in last_sel else DECL.sub(repl, piece))
        elif nxt == "{": last_sel = piece; result.append(piece)
        else: result.append(DECL.sub(repl, piece) if stack and not piece.strip().startswith("@") and ";" in piece else piece)
    return "".join(result)

if __name__ == "__main__":
    for rel in FILES:
        src = ROOT / rel
        out = src.with_suffix(".rtl.css")
        css = src.read_text(encoding="utf-8-sig")
        header = f"/* GENERATED by tools/build-rtl-css.py from {rel}. Do not edit; re-run the script. */\n"
        out.write_text(header + flip_css(css), encoding="utf-8")
        print("wrote", out.relative_to(ROOT.parent.parent.parent.parent))
