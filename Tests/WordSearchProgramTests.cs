using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using Xunit;
using Moq;
using WordSearch.ConsoleApp;
using WordSearch.FileLib;
using WordSearch.WordSearchLib;

namespace Tests
{
    public class WordSearchProgramTests : IDisposable
    {
        private TestUtilities _testUtilities;
        private readonly StringWriter _consoleOuput;
        private readonly TextWriter _originalConsoleOutput;
        private readonly FileOperations _fileOperations;
        private const string TEST_DIRECTORY = "Test_WordSearchProgram";

        public WordSearchProgramTests()
        {
            _testUtilities = new TestUtilities();
            _originalConsoleOutput = Console.Out;
            _consoleOuput = new StringWriter();
            _fileOperations = new FileOperations();

            Console.SetOut(_consoleOuput);
        }

        [Theory]
        [InlineData("ABC|DEF|GHI", "A B C \nD E F \nG H I \n")]
        [InlineData("ABCD|EFGH|IJKL|MNOP", "A B C D \nE F G H \nI J K L \nM N O P \n")]
        public void WriteGridToConsole_WhenGridArrayPassedIn_WritesArrayContentsToConsole(string gridSource, string expected)
        {
            //arrange
            string[,] grid = _testUtilities.StringToGrid(gridSource);
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            WordSearchProgram wordSearchProgram = new WordSearchProgram(consoleWrapper, _fileOperations);

            //act
            wordSearchProgram.WriteGridToConsole(grid, ConsoleColor.Gray, ConsoleColor.Black);

            var output = _consoleOuput.ToString();

            //assert
            Assert.True(expected == _consoleOuput.ToString());

        }

        ///<summary>This test uses a class, ConsoleWrapperMock, which inherits from ConsoleWrapper and overrides the color setting properties so that
        ///we can "see" the color changes in the output and confirm that the output to the console should appear correctly.
        ///fg: indicates setting the foreground color and bg: indicates setting the background color
        ///</summary>
        [Theory]
        [InlineData("ABC|DEF|GHI", 1, 1, "<fg:Gray><bg:Black>A B C \nD <fg:Black><bg:Gray>E<fg:Gray><bg:Black> F \nG H I \n")]
        [InlineData("ABC|DEF|GHI", 2, 0, "<fg:Gray><bg:Black>A B <fg:Black><bg:Gray>C<fg:Gray><bg:Black> \nD E F \nG H I \n")]
        public void WriteGridToConsole_WhenGridArrayAndHighlightCoordinatesPassedIn_WritesArrayContentsToConsoleWithColorChanges(string gridSource, int xcoord, int ycoord, string expected)
        {
            //arrange
            string[,] grid = _testUtilities.StringToGrid(gridSource);
            IConsoleWrapper consoleWrapper = new ConsoleWrapperMock();
            WordSearchProgram wordSearchProgram = new WordSearchProgram(consoleWrapper, _fileOperations);

            //act
            wordSearchProgram.WriteGridToConsole(grid, ConsoleColor.Gray, ConsoleColor.Black, new PointList(){ new Point(xcoord, ycoord)});

            var output = _consoleOuput.ToString();

            //assert
            Assert.True(expected == _consoleOuput.ToString());
        }

        [Theory]
        [InlineData("c:/dir/file1.txt|c:/dir/file2.txt|c:/dir/file3.txt", "(1) file1.txt\n(2) file2.txt\n(3) file3.txt\n")]
        [InlineData("c:/dir1/dir2/file1.txt|c:/dir1/dir2/file2.txt|c:/dir1/dir2/file3.txt", "(1) file1.txt\n(2) file2.txt\n(3) file3.txt\n")]
        public void WriteNumberedFileNamesToConsole_WhenWellFormedFilePathsArePassedIn_WritesNumberedFileNamesToConsole(string filePathDelimeteredArray, string expected)
        {
            //arrange
            string[] filePaths = filePathDelimeteredArray.Split('|');
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            IFileOperations fileOperations = new FileOperations();
            WordSearchProgram wordSearchProgram = new WordSearchProgram(consoleWrapper, fileOperations);

            //act
            wordSearchProgram.WriteNumberedFileNamesToConsole(filePaths);
            var output = _consoleOuput.ToString();

            //assert
            Assert.True(expected == _consoleOuput.ToString());

        }

        [Theory]
        [InlineData("WORD1,WORD2,WORD3", "A,B,C|D,E,F|G,H,I", "puzzle.txt")]
        [InlineData("WORD1,WORD2,WORD3,WORD4", "A,B,C,D|E,F,G,H|I,J,K,L|M,N,O,P", "puzzle.txt")]
        public void GetSearchStringsAndGridFromPuzzleFile_WhenFileExistsInCorrectFormat_ReturnsFirstRowAsSearchStringAndAllOthersAsStringArray(string searchWords, string fileRowsDelimeteredArray, string puzzleFileName)
        {
            //arrange
            string workingDir = _fileOperations.ApplicationBasePath(TestUtilities.APPLICATION_DIRECTORY) + "/" + TEST_DIRECTORY;
            CreatePuzzleFile(workingDir, searchWords, fileRowsDelimeteredArray, puzzleFileName);

            //need to remove the commas to provide meaningful input to StringToGrid
            string[,] expectedGrid = _testUtilities.StringToGrid(fileRowsDelimeteredArray.Replace(",",""));

            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            WordSearchProgram wordSearchProgram = new WordSearchProgram(consoleWrapper, _fileOperations);

            //act
            var (searchString, grid) = wordSearchProgram.ConvertPuzzleFileToSearchWordsAndGrid($"{workingDir}/{puzzleFileName}");

            //assert
            Assert.Equal(searchWords, searchString);
            Assert.Equal(expectedGrid, grid);
        }

        private void CreatePuzzleFile(string workingDir, string searchWords, string fileRowsDelimeteredArray, string puzzleFileName)
        {
            _testUtilities.CreateEmptyDirectory(workingDir);

            string[] puzzleRows = fileRowsDelimeteredArray.Split('|');  

            string[] puzzleForFile = new string[puzzleRows.Length + 1];
            puzzleForFile[0] = searchWords;
            for (int i = 0; i < puzzleRows.Length; i++)
            {
                puzzleForFile[i + 1] = puzzleRows[i];
            }

            File.WriteAllLines($"{workingDir}/{puzzleFileName}", puzzleForFile);
        }

        [Theory]
        [InlineData("AB,HEB", "A,B,C|D,E,F|G,H,I", "puzzle.txt","AB: (0,0),(1,0)\nHEB: (1,2),(1,1),(1,0)\n")]
        [InlineData("16AF", "1,2,3,4|5,6,7,8|9,0,A,B|C,D,E,F", "puzzle.txt","16AF: (0,0),(1,1),(2,2),(3,3)\n")]
        public void WriteSolvedPuzzleCoordinatesToConsole_WhenSearchWordFound_CoordinatesWrittenToConsole(string searchWords, string fileRowsDelimeteredArray, string puzzleFileName, string expected)
        {
            //arrange
            string workingDir = _fileOperations.ApplicationBasePath(TestUtilities.APPLICATION_DIRECTORY) + "/" + TEST_DIRECTORY;
            CreatePuzzleFile(workingDir, searchWords, fileRowsDelimeteredArray, puzzleFileName);

            IConsoleWrapper consoleWrapper = new ConsoleWrapperMock();
            WordSearchProgram wordSearchProgram = new WordSearchProgram(consoleWrapper, _fileOperations);

            //act
            var (searchString, grid) = wordSearchProgram.ConvertPuzzleFileToSearchWordsAndGrid($"{workingDir}/{puzzleFileName}");
            wordSearchProgram.WriteSolvedPuzzleCoordinatesToConsole(searchString, grid);
            var output = _consoleOuput.ToString();

            //assert
            Assert.True(expected == _consoleOuput.ToString());
        }

        private class ConsoleWrapperMock : ConsoleWrapper
        {
            public override ConsoleColor BackgroundColor
            { 
                get 
                {
                    return Console.BackgroundColor;
                }
                set
                {
                    Write($"<bg:{value.ToString()}>");
                    Console.BackgroundColor = value;
                }
            }

            public override ConsoleColor ForegroundColor
            { 
                get 
                {
                    return Console.ForegroundColor;
                }
                set
                {
                    Write($"<fg:{value.ToString()}>");
                    Console.ForegroundColor = value;
                }
            }
        }

        public void Dispose()
        {
            Console.SetOut(_originalConsoleOutput);

            string workingDir = _fileOperations.ApplicationBasePath(TestUtilities.APPLICATION_DIRECTORY) + "/" + TEST_DIRECTORY;
            DirectoryInfo di = new DirectoryInfo(workingDir);
            if (di.Exists)
            {
                di.Delete(true);
            }
        }


    }
}