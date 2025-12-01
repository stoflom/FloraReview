Application to open SQLite3 database, select some entries, display original text and modified text for editing and approval.
The selected entries can then be exported to a csv file using '\t' as delimiter.

The application assumes the database schema as in schemas.sql. It will update the descriptions table, columns:
ApprovedText, Status, Reviewer, Comments. The original- and AI modified text is not changed.

This software is freeware and is provided "as is" without any warranty. The author is not responsible for any damages
or losses caused by the use of this software. By using this software, you agree to these terms.

The source of this software is freely available here: https://github.com/stoflom/FloraReview

The FloraReview assembly is signed with a strong name key file using the following process:

1) Generate snk file
> sn -k sgKey.snk
move to solution directory, update .gitignore to ignore *.snk files 

2) Open the Project (not deployment project) properties, under build, select  
>Build>Strong naming>Tick Sign the output assembly to give it a strong name

3) Select the Strong Name Key File, use the browse button to find the sgKey.snk file.

4) Save, Clean, Rebuild the project.

5) Check: open Terminal view (PowerShell)
> sn -v .\FloraReview\bin\Release\net8.0-windows\FloraReview.dll

STRONG NAMING serves little purpose except making assemblies more unique