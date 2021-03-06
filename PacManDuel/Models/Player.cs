﻿using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using PacManDuel.Exceptions;

namespace PacManDuel.Models
{
    class Player
    {
        private readonly String _playerName;
        private readonly String _workingPath;
        private readonly String _executableFileName;
        private int _score;
        private int _numberOfPoisonPills;
        private Point _currentPosition;
        private char _symbol;

        public Player(String playerName, String workingPath, String executableFileName, char symbol)
        {
            _playerName = playerName;
            _workingPath = workingPath;
            _executableFileName = executableFileName;
            _score = 0;
            _numberOfPoisonPills = 1;
            _symbol = symbol;
        }

        public Maze GetMove(Maze maze, String outputFilePath, StreamWriter logFile)
        {
            var playerOutputFilePath = _workingPath + "\\" + Properties.Settings.Default.SettingBotOutputFileName;
            File.Delete(playerOutputFilePath);
            var startTime = DateTime.Now;
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = _workingPath,
                    FileName = _executableFileName,
                    Arguments = "\"" + outputFilePath + "\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            p.Start();
            var attemptFetchingMaze = true;
            while (attemptFetchingMaze)
            {
                if (File.Exists(playerOutputFilePath))
                {
                    if(!p.HasExited) p.Kill();
                    try
                    {
                        var mazeFromPlayer = new Maze(playerOutputFilePath);
                        return mazeFromPlayer;
                    }
                    catch (UnreadableMazeException e)
                    {
                        Console.WriteLine(e.ToString());
                        logFile.WriteLine("[GAME] : Unreadable maze from player " + _playerName);
                    }
                }
                if ((DateTime.Now - startTime).TotalSeconds > Properties.Settings.Default.SettingBotOutputTimeoutSeconds)
                {
                    attemptFetchingMaze = false;
                    if (!p.HasExited) p.Kill();
                    logFile.WriteLine("[GAME] : Timeout from player " + _playerName);
                }
                Thread.Sleep(100);
            }
            return null;
        }

        public void AddToScore(int score)
        {
            _score += score;
        }

        public int GetScore()
        {
            return _score;
        }

        public bool IsAllowedPoisonPillDrop()
        {
            return _numberOfPoisonPills > 0;
        }

        public void UsePoisonPill()
        {
            _numberOfPoisonPills--;
        }

        public String GetPlayerName()
        {
            return _playerName;
        }

        public Point GetCurrentPosition()
        {
            return _currentPosition;
        }

        public void SetCurrentPosition(Point coordinate)
        {
            _currentPosition = coordinate;
        }

        public char GetSymbol()
        {
            return _symbol;
        }

    }
}
