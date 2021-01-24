# Usage:

```sh
cvsTool.exe [argument] <inputfile>

Arguments:
-d:      Convert CVS file to JSON
-e:      Convert JSON to CVS
```

# Examples:
Convert CVS to JSON:
```cvsTool.exe -d swg_stringtable_en.cvs```

Convert JSON to CVS (required original cvs file!):
```cvsTool.exe -e swg_stringtable_en.json```
