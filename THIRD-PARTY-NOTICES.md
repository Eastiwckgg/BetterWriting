# Third-party notices

## FrequencyWords

The word lists in `src/dict/` (used to fix the keyboard layout) are built from
[FrequencyWords](https://github.com/hermitdave/FrequencyWords) by Hermit Dave,
files `content/2018/ru/ru_50k.txt` and `content/2018/en/en_50k.txt`.

They are licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).
Changes: only words made of letters were kept, lowercased, deduplicated and gzip-compressed
by `tools/make-dicts.ps1`. The derived files in `src/dict/` are shared under the same license.
