import { build } from "esbuild";
await build({
  entryPoints: ["src/editor.js"],
  bundle: true,
  minify: true,
  format: "iife",
  globalName: "BnCssEditor",
  target: ["es2019"],
  outfile: "../../BolNews.Web/wwwroot/lib/bn-css-editor/bn-css-editor.js",
  legalComments: "none"
});
console.log("built");
