# Third-party notices

Aioros.Graphs ports code from the following projects. Their notices are kept here as their licenses require.

| Component | Where it is used | License |
|---|---|---|
| [d3-array](https://github.com/d3/d3-array) 3.2.4 | `src/Aioros.Graphs/Arrays` (ticks, bisectors, extent, statistics, quantiles, bins, groups) | ISC |
| [d3-scale](https://github.com/d3/d3-scale) 4.0.2 | `src/Aioros.Graphs/Scales` (continuous, sequential, diverging, ordinal, quantize, quantile and threshold scales, time scales, tick formats) | ISC |
| [d3-shape](https://github.com/d3/d3-shape) 3.2.0 | `src/Aioros.Graphs/Shapes` (line, area, curves, arc, pie, stack, symbols, links, radial lines and areas) | ISC |
| [d3-hierarchy](https://github.com/d3/d3-hierarchy) 3.1.2 | `src/Aioros.Graphs/Hierarchies` (hierarchies, stratify, tree, cluster, treemap and its tilings, pack, partition) | ISC |
| [d3-chord](https://github.com/d3/d3-chord) 3.0.1 | `src/Aioros.Graphs/Chords` (chord layouts, ribbons and arrow ribbons) | ISC |
| [d3-force](https://github.com/d3/d3-force) 3.0.0 | `src/Aioros.Graphs/Forces` (simulation, link, many-body, center, collide, x and y forces) | ISC |
| [d3-quadtree](https://github.com/d3/d3-quadtree) 3.0.1 | `src/Aioros.Graphs/Forces/QuadTree.cs` (the Barnes–Hut and collision quadtree) | ISC |
| [d3-sankey](https://github.com/d3/d3-sankey) 0.12.3 | `src/Aioros.Graphs/Sankey` (sankey layout, sankeyLinkHorizontal), with the semantics of its d3-array 2.12.1 (`sum`, `min`, `max`) and d3-shape 1.3.7 / d3-path 1.0.9 (`linkHorizontal`, unrounded numbers) | BSD-3-Clause, Copyright 2015 Mike Bostock (notice below) |
| [d3-axis](https://github.com/d3/d3-axis) 3.0.0 | `src/Aioros.Graphs/Axes` and `src/Aioros.Graphs.Svg/SvgAxisWriter.cs` (axis layout and markup) | ISC |
| [d3-path](https://github.com/d3/d3-path) 3.1.0 | `src/Aioros.Graphs/Paths` and `src/Aioros.Graphs.Svg` (path commands and their text) | ISC |
| [quickselect](https://github.com/mourner/quickselect) (via d3-array) | `src/Aioros.Graphs/Arrays/QuickSelect.cs` | ISC, Copyright 2018 Vladimir Agafonkin |
| fdlibm (via V8) | `src/Aioros.Graphs/Scales/JsMath.cs` (`log`, `exp`, `log1p`, `expm1`) and `JsTrig.cs` (`sin`, `cos`), as V8 computes them | Sun Microsystems notice, below |
| [d3-color](https://github.com/d3/d3-color) 3.1.0 | `src/Aioros.Graphs/Colors` (color spaces, parsing, formatting) | ISC |
| [d3-interpolate](https://github.com/d3/d3-interpolate) 3.0.1 | `src/Aioros.Graphs/Interpolation` (number, color, spline, piecewise and zoom interpolators) | ISC |
| [d3-scale-chromatic](https://github.com/d3/d3-scale-chromatic) 3.1.0 | `src/Aioros.Graphs/Colors` (`ColorSchemes`, `SchemeInterpolator`, `SchemeData.cs`) | ISC; its ColorBrewer schemes Apache-2.0 |
| [d3-format](https://github.com/d3/d3-format) 3.1.2 | `src/Aioros.Graphs/Formatting` (number formats; `NumberLocales.cs` holds its locale definitions, generated from `d3-format/locale` by `tools/locales`) | ISC |
| [d3-time](https://github.com/d3/d3-time) 3.1.0 | `src/Aioros.Graphs/Time` (UTC and local intervals and ticks) | ISC |
| [d3-time-format](https://github.com/d3/d3-time-format) 4.1.0 | `src/Aioros.Graphs/Formatting` (UTC and local time formats; `TimeLocales.cs` holds its locale definitions, generated from `d3-time-format/locale` by `tools/locales`) | ISC |
| [d3](https://github.com/d3/d3) 7.9.0 | Not shipped: `tools/golden` runs it to generate the test fixtures, `bench/d3` to measure it | ISC |

## d3

Each d3 module carries the same license; the years differ per module (d3-array 2010-2023, d3-scale 2010-2021,
d3-shape 2010-2022, d3-axis 2010-2021, d3-path 2015-2022, d3-format 2010-2026, d3-time 2010-2022, d3-time-format 2010-2021,
d3-color 2010-2021, d3-hierarchy 2010-2021, d3-chord 2010-2021, d3-force 2010-2021, d3-quadtree 2010-2021, d3-interpolate 2010-2022, d3-scale-chromatic 2010-2024).

```
Copyright 2010-2023 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
```

## d3-sankey

d3-sankey 0.12.3 (and the d3-array 2.12.1, d3-shape 1.3.7 and d3-path 1.0.9 releases whose semantics the port follows
for it) carry the BSD 3-clause license:

```
Copyright 2015, Mike Bostock
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the author nor the names of contributors may be used to
  endorse or promote products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

## fdlibm

```
Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved.

Developed at SunSoft, a Sun Microsystems, Inc. business.
Permission to use, copy, modify, and distribute this
software is freely granted, provided that this notice
is preserved.

## ColorBrewer

d3-scale-chromatic's diverging and sequential ColorBrewer schemes (BrBG to YlOrRd, compiled into
`src/Aioros.Graphs/Colors/SchemeData.cs`) come from ColorBrewer, under the Apache License 2.0
(<http://www.apache.org/licenses/LICENSE-2.0>):

```
Apache-Style Software License for ColorBrewer software and ColorBrewer Color Schemes

Copyright 2002 Cynthia Brewer, Mark Harrower, and The Pennsylvania State University

Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
CONDITIONS OF ANY KIND, either express or implied. See the License for the
specific language governing permissions and limitations under the License.
```
