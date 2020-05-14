IF NOT EXIST "Artifacts" (
	MKDIR Artifacts
)

IF EXIST "Artifacts\test1.txt" (
	DEL "Artifacts\test1.txt"
)

ECHO abc > "Artifacts\test1.txt"