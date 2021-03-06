﻿using System;
using System.Drawing;
using System.IO;
using PacManDuel.Helpers;
using PacManDuel.Services;
using PacManDuel.Shared;

namespace PacManDuel.Models
{
    class Game
    {
        private readonly PlayerPool _playerPool;
        private readonly GameMarshaller _gameMarshaller;
        private Maze _maze;
        private int _iteration;
        private Player _currentPlayer;
        private readonly char _secondMazePlayer;

        public Game(Player playerA, Player playerB, String pathToInitialMaze)
        {
            _playerPool = new PlayerPool(playerA, playerB);
            _maze = new Maze(pathToInitialMaze);
            _gameMarshaller = new GameMarshaller();
            _iteration = 1;
            _secondMazePlayer = 'A';
        }

        public void Run(String folderPath)
        {
            var gamePlayDirectoryPath = Properties.Settings.Default.SettingPrimaryDriveName + "\\" + folderPath;
            Directory.CreateDirectory(gamePlayDirectoryPath);
            var outputFilePath = gamePlayDirectoryPath + "\\" + Properties.Settings.Default.SettingGamePlayFile;
            _maze.WriteMaze(outputFilePath);
            Player winner = null;
            var gameOutcome = Enums.GameOutcome.ProceedToNextRound;
            Directory.CreateDirectory(folderPath);
            Directory.CreateDirectory(folderPath + "\\" + Properties.Settings.Default.SettingReplayFolder);
            var logFile = new StreamWriter(folderPath + "\\" + Properties.Settings.Default.SettingMatchLogFileName);
            logFile.WriteLine("[GAME] : Match started");
            while (gameOutcome.Equals(Enums.GameOutcome.ProceedToNextRound))
            {
                _currentPlayer = _playerPool.GetNextPlayer();
                var mazeFromPlayer = _currentPlayer.GetMove(_maze, gamePlayDirectoryPath + "\\" + Properties.Settings.Default.SettingGamePlayFile, logFile);
                if (mazeFromPlayer != null)
                {
                    var mazeValidationOutcome = GetMazeValidationOutcome(logFile, mazeFromPlayer);
                    if (mazeValidationOutcome.Equals(Enums.MazeValidationOutcome.ValidMaze))
                    {
                        var opponentPosition = _maze.FindCoordinateOf(Properties.Settings.Default.SymbolPlayerB);
                        var previousPosition = _maze.FindCoordinateOf(Properties.Settings.Default.SymbolPlayerA);
                        var currentPosition = mazeFromPlayer.FindCoordinateOf(Properties.Settings.Default.SymbolPlayerA);
                        var turnOutcome = GetTurnOutcome(mazeFromPlayer, currentPosition, previousPosition, opponentPosition, logFile);
                        if (!turnOutcome.Equals(Enums.TurnOutcome.MoveMadeAndDroppedPoisonPillIllegally))
                        {
                            gameOutcome = GetGameOutcome(logFile, gameOutcome, turnOutcome);
                            winner = DeterminIfWinnerWinner(gameOutcome, mazeFromPlayer, winner);
                        }
                        else gameOutcome = ProcessIllegalMove(logFile, gameOutcome, ref winner);
                    }
                    else gameOutcome = ProcessIllegalMove(logFile, gameOutcome, ref winner);
                    
                    _maze.WriteMaze(gamePlayDirectoryPath + "\\" + Properties.Settings.Default.SettingGamePlayFile);
                    CreateIterationStateFile(folderPath);
                    _iteration++;
                    _maze.Print();
                }
                else gameOutcome = ProcessIllegalMove(logFile, gameOutcome, ref winner);
            }

            CreateMatchInfo(gameOutcome, winner, logFile);
            logFile.Close();
            var replayMatchOutcome = new StreamWriter(folderPath + "\\replay\\matchinfo.out");
            CreateMatchInfo(gameOutcome, winner, replayMatchOutcome);
            replayMatchOutcome.Close();
        }

        private Enums.GameOutcome ProcessIllegalMove(StreamWriter logFile, Enums.GameOutcome gameOutcome, ref Player winner)
        {
            logFile.WriteLine("[GAME] : Illegal move made by " + _currentPlayer.GetPlayerName());
            gameOutcome = Enums.GameOutcome.IllegalMazeState;
            winner = _playerPool.GetNextPlayer();
            return gameOutcome;
        }

        private Player DeterminIfWinnerWinner(Enums.GameOutcome gameOutcome, Maze mazeFromPlayer, Player winner)
        {
            if (gameOutcome.Equals(Enums.GameOutcome.ProceedToNextRound))
            {
                mazeFromPlayer.SwapPlayerSymbols();
                _maze = mazeFromPlayer;
            }
            else if (gameOutcome.Equals(Enums.GameOutcome.NoScoringMaxed))
            {
                winner = _playerPool.GetNextPlayer();
            }
            else
            {
                winner = GameJudge.DetermineWinner(_playerPool);
            }
            return winner;
        }

        private Enums.GameOutcome GetGameOutcome(StreamWriter logFile, Enums.GameOutcome gameOutcome, Enums.TurnOutcome turnOutcome)
        {
            logFile.WriteLine("[GAME] : Player " + _currentPlayer.GetPlayerName() + " has " + _currentPlayer.GetScore() + " points");
            logFile.WriteLine("[TURN] : Moved to " + _currentPlayer.GetCurrentPosition().X + ", " + _currentPlayer.GetCurrentPosition().Y);
            gameOutcome = _gameMarshaller.ProcessGame(_maze, turnOutcome);
            logFile.WriteLine("[TURN] : " + _gameMarshaller.GetTurnsWithoutPointsInfo() + " turns without points");
            logFile.WriteLine("[GAME] : " + Enum.GetName(typeof (Enums.GameOutcome), gameOutcome));
            return gameOutcome;
        }

        private Enums.TurnOutcome GetTurnOutcome(Maze mazeFromPlayer, Point currentPosition, Point previousPosition,
            Point opponentPosition, StreamWriter logFile)
        {
            var turnOutcome = TurnMarshaller.ProcessMove(mazeFromPlayer, _maze, currentPosition, previousPosition, opponentPosition, _currentPlayer);
            logFile.WriteLine("[TURN] : " + Enum.GetName(typeof (Enums.TurnOutcome), turnOutcome));
            logFile.WriteLine("[TURN] : " + _currentPlayer.GetPlayerName() + " at " + currentPosition.X + ", " +
                              currentPosition.Y);
            return turnOutcome;
        }

        private Enums.MazeValidationOutcome GetMazeValidationOutcome(StreamWriter logFile, Maze mazeFromPlayer)
        {
            logFile.WriteLine("[GAME] : Received maze from player " + _currentPlayer.GetPlayerName());
            var mazeValidationOutcome = (MazeValidator.ValidateMaze(mazeFromPlayer, _maze));
            logFile.WriteLine("[MAZE] : " + Enum.GetName(typeof (Enums.MazeValidationOutcome), mazeValidationOutcome));
            return mazeValidationOutcome;
        }


        private void CreateIterationStateFile(String folderPath)
        {
            var replayFile =
                new StreamWriter(folderPath + "\\" + Properties.Settings.Default.SettingReplayFolder + "\\iteration" +
                                 _iteration + Properties.Settings.Default.SettingStateFileExtension);
            var mazeForFile = new Maze(_maze);
            if (_secondMazePlayer.Equals(_currentPlayer.GetSymbol()))
                mazeForFile.SwapPlayerSymbols();
            replayFile.Write(mazeForFile.ToFlatFormatString());
            replayFile.Close();
        }

        private void CreateMatchInfo(Enums.GameOutcome gameOutcome, Player winner, StreamWriter file)
        {
            foreach (var player in _playerPool.GetPlayers())
            {
                file.WriteLine("PLAYER:" + player.GetSymbol() + "," + player.GetPlayerName() + "," + player.GetScore());
            }
            if (winner == null)
                file.WriteLine("GAME: DRAW," + Enum.GetName(typeof(Enums.GameOutcome), gameOutcome) + "," + _iteration);
            else
                file.WriteLine("GAME: " + winner.GetSymbol() + "," + Enum.GetName(typeof(Enums.GameOutcome), gameOutcome) + "," + _iteration);
        }

    }
}
