﻿using System.Drawing;
using NLog;
using PacManDuel.Models;
using PacManDuel.Shared;

namespace PacManDuel.Services
{
    class TurnMarshaller
    {
        public static Enums.TurnOutcome ProcessMove(Maze currentMaze, Maze previousMaze, Point currentPosition, Point previousPosition, Point opponentPosition, Player currentPlayer)
        {
            currentPlayer.SetCurrentPosition(currentPosition);

            if (IsMoveMadeAndScoredPoint(previousMaze, currentPosition))
            {
                currentPlayer.AddToScore(Properties.Settings.Default.SettingPointsPerPill);
                return Enums.TurnOutcome.MoveMadeAndPointScored;
            }

            if (IsMoveMadeAndScoredBonusPoint(previousMaze, currentPosition))
            {
                currentPlayer.AddToScore(Properties.Settings.Default.SettingPointsPerBonusPill);
                return Enums.TurnOutcome.MoveMadeAndBonusPointScored;
            }
            
            if (IsMoveMadeAndDiedFromPoisonPill(previousMaze, currentPosition))
            {
                currentMaze.SetSymbol(currentPosition.X, currentPosition.Y, Properties.Settings.Default.SymbolEmpty);
                currentMaze.SetSymbol(Properties.Settings.Default.MazeCenterX, Properties.Settings.Default.MazeCenterY, Properties.Settings.Default.SymbolPlayerA);
                return Enums.TurnOutcome.MoveMadeAndDiedFromPoisonPill;
            }

            if (IsMoveMadeAndKilledOpponent(currentPosition, opponentPosition))
            {
                currentMaze.SetSymbol(Properties.Settings.Default.MazeCenterX, Properties.Settings.Default.MazeCenterY, Properties.Settings.Default.SymbolPlayerB);
                return Enums.TurnOutcome.MoveMadeAndKilledOpponent;
            }

            if (IsMoveMadeAndDroppedPoisonPill(currentMaze, previousPosition))
            {
                if (!currentPlayer.IsAllowedPoisonPillDrop())
                    return Enums.TurnOutcome.MoveMadeAndDroppedPoisonPillIllegally;

                currentPlayer.UsePoisonPill();
                return Enums.TurnOutcome.MoveMadeAndDroppedPoisonPill;
            }

            return (int)Enums.TurnOutcome.MoveMade;
        }

        private static bool IsMoveMadeAndScoredPoint(Maze previousMaze, Point currentPosition)
        {
            return previousMaze.GetSymbol(currentPosition.X, currentPosition.Y).Equals(Properties.Settings.Default.SymbolPill);
        }

        private static bool IsMoveMadeAndScoredBonusPoint(Maze previousMaze, Point currentPosition)
        {
            return previousMaze.GetSymbol(currentPosition.X, currentPosition.Y).Equals(Properties.Settings.Default.SymbolPowerPill);
        }

        private static bool IsMoveMadeAndDiedFromPoisonPill(Maze previousMaze, Point currentPosition)
        {
            return previousMaze.GetSymbol(currentPosition.X, currentPosition.Y).Equals(Properties.Settings.Default.SymbolPoisonPill);
        }

        private static bool IsMoveMadeAndKilledOpponent(Point currentPosition, Point opponentPosition)
        {
            return currentPosition.X.Equals(opponentPosition.X) && currentPosition.Y.Equals(opponentPosition.Y);
        }

        private static bool IsMoveMadeAndDroppedPoisonPill(Maze currentMaze, Point previousPosition)
        {
            return currentMaze.GetSymbol(previousPosition.X, previousPosition.Y).Equals(Properties.Settings.Default.SymbolPoisonPill);
        }

    }
}
