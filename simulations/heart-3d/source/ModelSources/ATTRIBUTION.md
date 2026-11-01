# Anatomy model attribution

The anatomy meshes in `Assets/Resources/Atlas/Heart.bytes`, the derived meshes in
`Assets/Models`, and `ModelSources/AtlasHeartSource.blend` are adapted from:

**Z-Anatomy — The libre 3D atlas of anatomy**
Gauthier Kervyn (design, 3D, anatomy), Marcin Zielinski (Blender tools),
and the contributors credited by the project.

- Source: https://github.com/Z-Anatomy/Models-of-human-anatomy
- Source archive: `Z-Anatomy.zip`, `Z-Anatomy/Startup.blend`.
- License: Creative Commons Attribution-ShareAlike 4.0 International.
- License terms: https://creativecommons.org/licenses/by-sa/4.0/
- Upstream license: https://github.com/Z-Anatomy/Models-of-human-anatomy/blob/master/License.txt

Underlying model attribution requested by Z-Anatomy:

**BodyParts3D — The Database Center for Life Science — CC-BY-SA 2.1 Japan**
Original model credited to Kousaku Okubo and BodyParts3D contributors.
Original database: https://dbarchive.biosciencedbc.jp/en/bodyparts3d/desc.html

Adaptations made for this simulation: extraction of 33 named cardiac/vessel
structures; evaluation of subdivision and curve geometry; trimming long vessel
branches; transformation into Unity axes at 40 mm per model unit; recalculated
normals; rendering materials; interactive cutting; animation of the six native
semilunar cusps. The AV valve leaflets and their animated chordae are reconstructed
illustrations positioned using the atlas, because the selected upstream atlas
does not supply complete animated AV valve sets. The upstream papillary muscles
are retained.

The adapted anatomy model assets are shared under CC BY-SA 4.0. Preserve these
credits and the same license when redistributing adapted model assets. This notice
does not claim that the upstream creators endorsed the simulation.

The binary format stores each named structure as vertices and triangle indices;
it contains no executable code from the downloaded Blender atlas. The original
atlas's embedded script was disabled during extraction.
