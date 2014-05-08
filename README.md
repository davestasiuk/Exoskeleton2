<h1>Exoskeleton2</h1>

<h2>Description</h2>

Exoskeleton2 is collection of mesh generation components for <a href="www.grasshopper3d.com">Grasshopper</a>. Its components are used to either wrap or thicken existing geometric elements in the modelling space. It also relies upon the libraries <a href="https://github.com/mcneel/rhinocommon">Rhinocommon</a> for functionality in Rhino and <a href="https://github.com/Dan-Piker/Plankton">Plankton</a> for n-gon, half-edge mesh operations.

<h2>Current Components</h2>
<ul>
<li>
<strong>Cytoskeleton</strong> - This is a wireframe thickening tool, intended for 3d printing use. It works exclusively on lines which form the edges of meshes. The additional connectivity information present in this case makes it possible to produce an output with all quads, and moreover, a quad mesh with all even valence vertices. Because it works on a Plankton mesh, the input can be made of ngons. We can also input a triangular mesh, apply the dual operation, and then thicken the edges of the resulting polygon mesh.
</li>
<li>
<strong>Exo Wireframe</strong> - Another thickening tool, this transforms node-based wireframes into meshes. Input a network of lines to return a solid mesh.
</li>
</ul>

<h2>Future Components</h2>
<ul>
<li>
<strong>Exo Curve</strong>
</li>
<li>
<strong>Isosurface Wrapper</strong>
</li>
</ul>

<h2>License</h2>

<p>Exoskeleton2 is an <strong>open source</strong> library and is licensed under the <a href="http://www.gnu.org/licenses/lgpl.html">Gnu Lesser General Public License</a> (LGPL). We chose this license because we believe that it will encourage those who <strong>improve</strong> the library to <strong>share</strong> their work whilst not requiring the same of those who simply <strong>use</strong> the library in their software.</p>

<pre><code>Exoskeleton2 is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as
published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

Exoskeleton2 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with Plankton.  If not, see
&lt;http://www.gnu.org/licenses/&gt;.
</code></pre>

<p>Exoskeleton2 © 2014 Daniel Piker and David Stasiuk.</p>
