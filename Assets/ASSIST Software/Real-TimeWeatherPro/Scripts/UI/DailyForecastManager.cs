//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;
using RealTimeWeather.Classes;
using UnityEngine;

namespace RealTimeWeather.UI
{
    public class DailyForecastManager : MonoBehaviour
    {
        #region Private Const Variables
        private const int kWeekDays = 7;
        #endregion

        #region Private Variables
        private List<WeatherData> _dailyWeatherData;
        private List<GameObject> _tableCells;

        private int _weekDayCounter = 1;

        private DateTime _startDateTime;
        private int _currentDay = 0;
        private int _daysPassed = 0;
        private int _dayIndex = 0;
        #endregion

        #region Public Variables
        public GameObject weeklyDataTable;
        public GameObject tableCellPrefab;
        #endregion

        #region Private Variables
        private void ClearTableCells()
        {
            foreach (Transform child in weeklyDataTable.transform)
            {
                Destroy(child.gameObject);
            }
        }
        #endregion

        #region Public Methods
        public void OnForecastDayPassed()
        {
            _currentDay++;
            _weekDayCounter++;
            if (_currentDay >= _dailyWeatherData.Count || _weekDayCounter >= kWeekDays)
            {
                SetTableDataForCurrentWeek();

                // Reset weekday counter because we begin a new week of simulation
                _weekDayCounter = 0;
                // Reset the forecast days counter in case we are looping through the list
                if (_currentDay >= _dailyWeatherData.Count)
                {
                    _currentDay = 0;
                }
            }
            else
            {
                // Highlight the next cell
                _tableCells[_weekDayCounter - 1].GetComponent<CellContents>().SetHighlight(false);
                _tableCells[_weekDayCounter].GetComponent<CellContents>().SetHighlight(true);
            }
        }

        /// <summary>
        /// Set weather data for all days
        /// </summary>
        /// <param name="weatherData">A list with the Forecast data</param>
        public void SetForecastData(List<WeatherData> weatherData)
        {
            _daysPassed = 0;
            _dayIndex = 0;
            _weekDayCounter = 0;
            _startDateTime = weatherData[0].DateTime;
            _dailyWeatherData = weatherData;
            SetTableDataForCurrentWeek();
        }

        /// <summary>
        /// Adds the forecast data for the current week
        /// </summary>
        public void SetTableDataForCurrentWeek()
        {
            // Clear always before setting new data
            ClearTableCells();

            // Populate with data
            _tableCells = new List<GameObject>();
            for (int i = 0; i < kWeekDays; i++)
            {
                if (_dayIndex < _dailyWeatherData.Count)
                {
                    var newDayCell = Instantiate(tableCellPrefab, weeklyDataTable.transform);
                    CellContents cellContents = newDayCell.GetComponent<CellContents>();
                    cellContents.SetDefaultContents();

                    // Calculates the current day based on the date from the first forecast data in case we are looping through the list
                    WeatherData newWeatherData = Utilities.NewWeatherDataInstance(_dailyWeatherData[_dayIndex]);
                    newWeatherData.DateTime = _startDateTime.AddDays(_daysPassed);

                    cellContents.SetCellContents(newWeatherData);
                    _dayIndex++;
                    _daysPassed++;

                    // Keep a record of the newly created cell
                    _tableCells.Add(newDayCell);
                }
            }

            // Reset the counter in case we are looping through the list
            if (_dayIndex >= _dailyWeatherData.Count)
            {
                _dayIndex = 0;
            }

            // Highlight the first cell
            _tableCells[0].GetComponent<CellContents>().SetHighlight(true);
        }
        #endregion
    }
}
