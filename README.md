# XOR_Encryptor_Decryptor


Write a program that encrypts / decrypts the specified file with the XOR encryption algorithm. Key length at least 6 characters.<br/>
The user should be able to specify the size of the data blocks being processed in bytes. The default is 4096 bytes.<br/>
The algorithm works as follows:<br/>
1. Program open file.<br/>
In the cycle:<br/>
2. Reads N bytes from the file.<br/>
3. Encrypts / decrypts.<br/>
4. Returns the position in the file back to the number of bytes read.<br/>
5. Write encrypted / decrypted data to a file.<br/>
6. Close the file.<br/>
The progress of encryption / decryption should be displayed by ProgressBar.<br/>
When mouse over the ProgressBar, prompt should be displayed - encryption / decryption speed in Kb / sec.<br/>
The user has the ability to cancel the operation.<br/>
If the user canceled the encryption / decryption operation, you must return the file to its original state.<br/>
The GUI must respond to user actions at the time of operation.
