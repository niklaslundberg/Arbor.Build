IF NOT EXIST "Artifacts" (
	MKDIR Artifacts
)

IF EXIST "Artifacts\test2.txt" (
	DEL "Artifacts\test2.txt"
)

ECHO abc > "Artifacts\test2.txt"