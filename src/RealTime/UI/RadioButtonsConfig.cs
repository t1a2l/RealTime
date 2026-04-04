namespace RealTime.UI
{
    using System.ComponentModel;

    internal class RadioButtonsConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum ModeType
        {
            ClearStuckCitizensSchedule,
            ClearStuckTouristsInHotels,
            ClearStuckCitizensInClosedBuildings,
            ClearFireBurnTimeManager,
            ClearBuildingsWorkTimePrefabs,
            ClearBuildingWorkTimeGlobalSettings,
            ResetBuildingsGarbageBuffer,
            ResetBuildingsMailBuffer,
            ResetBuildingsCrimeBuffer
        }

        public ModeType SelectedMode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(SelectedMode));
                    OnPropertyChanged(null);  // Refresh ALL bools
                }
            }
        } = ModeType.ClearStuckCitizensSchedule;

        public bool IsClearStuckCitizensScheduleMode
        {
            get => SelectedMode == ModeType.ClearStuckCitizensSchedule;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearStuckCitizensSchedule;
                }
            }
        }

        public bool IsClearStuckTouristsInHotelsMode
        {
            get => SelectedMode == ModeType.ClearStuckTouristsInHotels;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearStuckTouristsInHotels;
                }
            }
        }

        public bool IsClearStuckCitizensInClosedBuildingsMode
        {
            get => SelectedMode == ModeType.ClearStuckCitizensInClosedBuildings;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearStuckCitizensInClosedBuildings;
                }
            }
        }

        public bool IsClearFireBurnTimeManagerMode
        {
            get => SelectedMode == ModeType.ClearFireBurnTimeManager;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearFireBurnTimeManager;
                }
            }
        }

        public bool IsClearBuildingsWorkTimePrefabsMode
        {
            get => SelectedMode == ModeType.ClearBuildingsWorkTimePrefabs;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearBuildingsWorkTimePrefabs;
                }
            }
        }

        public bool IsClearBuildingWorkTimeGlobalSettingsMode
        {
            get => SelectedMode == ModeType.ClearBuildingWorkTimeGlobalSettings;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ClearBuildingWorkTimeGlobalSettings;
                }
            }
        }

        public bool IsResetBuildingsGarbageBufferMode
        {
            get => SelectedMode == ModeType.ResetBuildingsGarbageBuffer;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ResetBuildingsGarbageBuffer;
                }
            }
        }

        public bool IsResetBuildingsMailBufferMode
        {
            get => SelectedMode == ModeType.ResetBuildingsMailBuffer;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ResetBuildingsMailBuffer;
                }
            }
        }

        public bool IsResetBuildingsCrimeBufferMode
        {
            get => SelectedMode == ModeType.ResetBuildingsCrimeBuffer;
            set
            {
                if (value)
                {
                    SelectedMode = ModeType.ResetBuildingsCrimeBuffer;
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

}
