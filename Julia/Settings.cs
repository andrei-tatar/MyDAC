using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Julia.Drivers;

namespace Julia
{
    [Serializable]
    public enum InputType
    {
        Generic,
        Bluetooth,
        Music,
        Pc,
    }

    [Serializable]
    public class InputDescriptor
    {
        public string Name { get; set; }
        public InputType Type { get; set; }
        public int InputIndex { get; set; }
        public int Id { get; set; }
        public bool Visible { get; set; }

        public InputDescriptor()
        {
            Visible = true;
        }
    }

    [Serializable]
    public class Settings
    {
        private const string FileName = @"settings.xml";

        private static Settings _instance;
        public static Settings Instance { get { return _instance ?? (_instance = Load()); } }

        private int _selectedInput, _volume, _brightness, _turnOffScreenTimeout;
        private bool _hasChanges;
        private bool _volumeInDb;
        private int _menuSpeed;

        public List<InputDescriptor> Inputs { get; set; }

        public int SelectedInput
        {
            get { return _selectedInput; }
            set
            {
                if (_selectedInput == value) return;
                _selectedInput = value;
                _hasChanges = true;
            }
        }
        public int Volume
        {
            get { return _volume; }
            set
            {
                if (_volume == value) return;
                _volume = value;
                _hasChanges = true;
            }
        }
        public bool VolumeInDb
        {
            get { return _volumeInDb; }
            set
            {
                if (value == _volumeInDb) return;
                _volumeInDb = value;
                _hasChanges = true;
            }
        }
        public int Brightness
        {
            get { return _brightness; }
            set
            {
                if (_brightness == value) return;
                _brightness = value;
                _hasChanges = true;
            }
        }
        public int TurnOffScreenTimeout
        {
            get { return _turnOffScreenTimeout; }
            set
            {
                if (value == _turnOffScreenTimeout) return;
                _turnOffScreenTimeout = value;
                _hasChanges = true;
            }
        }
        public int MenuSpeed
        {
            get { return _menuSpeed; }
            set
            {
                if (value == _menuSpeed) return;
                _menuSpeed = value;
                _hasChanges = true;
            }
        }
        private Settings()
        {
            Inputs = new List<InputDescriptor>();
            Brightness = 0x40;
        }

        private static Settings Default()
        {
            var settings = new Settings();

            var id = 0;

            for (var i = 0; i < JuliaSound.NumberOfInputs - 1; i++)
            {
                settings.Inputs.Add(
                    new InputDescriptor
                        {
                            Name = i == 0 ? "Pc" : "Input " + (i + 1),
                            Type = i == 0 ? InputType.Pc : InputType.Generic,
                            InputIndex = i,
                            Id = id++,
                        });
            }

            settings.Inputs.Add(
                new InputDescriptor
                {
                    Name = "Bluetooth",
                    Type = InputType.Bluetooth,
                    InputIndex = JuliaSound.NumberOfInputs - 1,
                    Id = id++,
                });

            settings.Inputs.Add(
                new InputDescriptor
                {
                    Name = "Music",
                    Type = InputType.Music,
                    InputIndex = JuliaSound.NumberOfInputs - 1,
                    Id = id,
                });

            settings.Volume = (JuliaSound.VolumeMax - JuliaSound.VolumeMin) / 2 + JuliaSound.VolumeMin;
            settings.VolumeInDb = true;
            settings.TurnOffScreenTimeout = 5;
            settings.MenuSpeed = 0;

            return settings;
        }

        public static void Save()
        {
            if (!Instance._hasChanges) return;
            var serializer = new XmlSerializer(typeof(Settings));
            using (var file = File.Create(FileName))
            {
                serializer.Serialize(file, Instance);
                Instance._hasChanges = false;
            }
        }

        private static Settings Load()
        {
            var serializer = new XmlSerializer(typeof(Settings));
            if (File.Exists(FileName))
            {
                FileStream file = null;
                try
                {
                    file = File.Open(FileName, FileMode.Open, FileAccess.Read);
                    return (Settings)serializer.Deserialize(file);
                }
                catch (Exception)
                {
                    return Default();
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            }
            return Default();
        }
    }
}
