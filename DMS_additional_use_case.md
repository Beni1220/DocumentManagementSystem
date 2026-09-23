GitHub link: https://github.com/Beni1220/DocumentManagementSystem



additonal UseCase:



UC1: Document Versioning



Goal: A user can keep several versions of a document, go back and forth between the different versions, without losing anything.



Actor: User



Preconditions: Document has to exist.



Trigger: The user uploads a new file for an existing document



Main flow:

1. The user uploads a file
2. The system validates the input (document exists, is it the same document, allowed type)
3. The system creates a new DocumentVersion with the next version number
4. The system stores the file and the metadata.



Alternative flows:

A1: Open an old version: The user sees the list of all versions of the document and can open them

A2: Delete the document: All its versions are deleted with it



Exceptions

E1: Document not found

E2: Verison not found

E3: Storing the file fails



Postcondition

Success: a new version exists, old versions are accessible

Failure: no data was changed.

