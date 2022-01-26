Mure: MUlti-valued Regular Expressions
======================================

[![Build Status](https://img.shields.io/github/workflow/status/r3c/mure/validate/master)](https://github.com/r3c/mure/actions)
[![license](https://img.shields.io/github/license/r3c/mure.svg)](https://opensource.org/licenses/MIT)

Overview
--------

Mure is a text stream matching engine with support for a subset of regular
expressions, able to bind several patterns to a corresponding value and return
the one value associated to matched pattern. This allows forward-only text
parsing while mapping each matched sequence to corresponding value, which is a
efficient solution for tokenization tasks e.g. when writing lexers.

Usage
-----

There is no user documentation available yet, sorry!

Here is a simple code sample:

```csharp
var matcher = Compiler
    .CreateFromRegex<LexemType>()
    .AddPattern("[0-9]+", LexemType.Number)
    .AddPattern("\\+", LexemType.Plus)
    .AddPattern("-", LexemType.Minus)
    .Compile();

using (var reader = new StringReader("27+32-4"))
{
    var iterator = matcher.Open(reader);

    while (iterator.TryMatchNext(out var match))
        Console.WriteLine($"Matched {match.Value}: {match.Capture}");
}
```

Running this code will print to standard output:

```
Matched Number: 27
Matched Plus: +
Matched Number: 32
Matched Minus: -
Matched Number: 4
```

Licence
-------

This project is open-source, released under MIT licence. See `LICENSE.md` file
for details.

Author
------

[Rémi Caput](http://remi.caput.fr/) (github.com+mure [at] mirari [dot] fr)
