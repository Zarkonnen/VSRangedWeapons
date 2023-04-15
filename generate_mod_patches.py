patchline = """
{
	file: "REPLACE",
	op: "add",
	path: "/server/behaviors/-",
	value: { code: "poisonable" }
},
{
	file: "REPLACE",
	op: "add",
	path: "/client/behaviors/-",
	value: { code: "poisonable" }
},
"""
with open("patchables.txt") as f:
	with open("mod_animal_patches.json", "w") as f2:
		f2.write("[\n")
		for l in f:
			f2.write(patchline.replace("REPLACE", l.replace("\n", "")))
		f2.write("]")
