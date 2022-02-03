# Unit Alert

Unit Alert is a utility which can monitor text files and alert the user using a sound whenever the selected word/phrase appears in said text file.

## Usage

This section provides information about using Unit Alert.

### Selecting a text file
In order to use Unit Alert, you first need to select the text file you want to watch. In the case of RageMP, that file is located in *"RAGE/clientdata/console.txt"*.

If you wish to use the tool for other services besides RAGEMP, you need to keep in mind that Unit Alert reads the text file line by line, so if your service writes a continuous string with no new lines, this will not work.

### Selecting a sound file

You can select any sound file you wish, but preferably in the **.mp3** format, as Unit Alert was not tested with other kinds of audio formats. It is also recommended the audio file is relatively short, as you do not receive additional alerts if there is a sound currently playing.

### Selecting the words/phrases

Unit alert can match any phrases, case insensitive. If you wish to match for multiple words/phrases, you need to separate them using a semicolon "**;**".

#### Examples

>Word 1;Hello;Testing with a sentence

This input will match everything between the semicolons.

### Locking in your choice

After deciding on your words, and files, click on the "Activate" button.