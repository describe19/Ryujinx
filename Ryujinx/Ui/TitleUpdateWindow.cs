using Gtk;
using JsonPrettyPrinterPlus;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibHac;
using Utf8Json;
using Utf8Json.Resolvers;

using GUI = Gtk.Builder.ObjectAttribute;

namespace Ryujinx.Ui
{
    public class TitleUpdateWindow : Window
    {
        private string              _titleId;
        private VirtualFileSystem   _virtualFileSystem;
        private TitleUpdateMetadata _titleUpdateWindowData;

        private Dictionary<RadioButton, string> _radioButtonToPathDictionary = new Dictionary<RadioButton, string>();

#pragma warning disable CS0649
#pragma warning disable IDE0044
        [GUI] Box         _availableUpdatesBox;
        [GUI] RadioButton _noUpdateRadioButton;
#pragma warning restore CS0649
#pragma warning restore IDE0044

        public TitleUpdateWindow(string titleId, VirtualFileSystem virtualFileSystem) : this(new Builder("Ryujinx.Ui.TitleUpdateWindow.glade"), titleId, virtualFileSystem) { }

        private TitleUpdateWindow(Builder builder, string titleId, VirtualFileSystem virtualFileSystem) : base(builder.GetObject("_titleUpdateWindow").Handle)
        {
            builder.Autoconnect(this);

            _titleId           = titleId;
            _virtualFileSystem = virtualFileSystem;

            try
            {
                using (Stream stream = File.OpenRead(System.IO.Path.Combine(_virtualFileSystem.GetBasePath(), "games", _titleId, "updates.json")))
                {
                    IJsonFormatterResolver resolver = CompositeResolver.Create(StandardResolver.AllowPrivateSnakeCase);

                    _titleUpdateWindowData = JsonSerializer.Deserialize<TitleUpdateMetadata>(stream, resolver);
                }
            }
            catch
            {
                _titleUpdateWindowData = new TitleUpdateMetadata
                {
                    Selected = "",
                    Paths    = new List<string>()
                };
            }

            foreach (string path in _titleUpdateWindowData.Paths)
            {
                AddUpdate(path);
            }

            foreach (KeyValuePair<RadioButton, string> keyValuePair in _radioButtonToPathDictionary)
            {
                if (keyValuePair.Value == _titleUpdateWindowData.Selected)
                    keyValuePair.Key.Active = true;
            }
        }

        private void AddUpdate(string path)
        {
            if (File.Exists(path))
            {
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    PartitionFileSystem nsp = new PartitionFileSystem(file.AsStorage());

                    foreach (DirectoryEntryEx fileEntry in nsp.EnumerateEntries("/", "*.nca"))
                    {
                        nsp.OpenFile(out IFile ncaFile, fileEntry.FullPath, OpenMode.Read).ThrowIfFailure();

                        Nca nca = new Nca(_virtualFileSystem.KeySet, ncaFile.AsStorage());

                        if (GetBaseTitleId(nca.Header.TitleId.ToString("x16")) == _titleId)
                        {
                            if (nca.Header.ContentType == NcaContentType.Control)
                            {
                                nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None).OpenFile(out IFile nacpFile, "/control.nacp", OpenMode.Read).ThrowIfFailure();
                                Nacp controlData = new Nacp(nacpFile.AsStream());

                                RadioButton radioButton = new RadioButton($"Version {controlData.DisplayVersion} - {path}");
                                radioButton.JoinGroup(_noUpdateRadioButton);

                                _availableUpdatesBox.Add(radioButton);
                                _radioButtonToPathDictionary.Add(radioButton, path);

                                radioButton.Show();
                            }
                        }
                        else
                        {
                            GtkDialog.CreateErrorDialog("The specified file does not contain an update for the selected title!");
                            break;
                        }
                    }
                }
            }
        }

        private string GetBaseTitleId(string titleId)
        {
            return $"{titleId.Substring(0, titleId.Length - 3)}000";
        }

        private void AddButton_Clicked(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Select update files", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Load", ResponseType.Accept)
            {
                SelectMultiple = true,
                Filter         = new FileFilter()
            };
            fileChooser.SetPosition(WindowPosition.Center);
            fileChooser.Filter.AddPattern("*.nsp");

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                foreach (string path in fileChooser.Filenames)
                {
                    AddUpdate(path);
                }
            }

            fileChooser.Dispose();
        }

        private void RemoveButton_Clicked(object sender, EventArgs args)
        {
            foreach (RadioButton radioButton in _noUpdateRadioButton.Group)
            {
                if (radioButton.Label != "No Update" && radioButton.Active)
                {
                    _availableUpdatesBox.Remove(radioButton);
                    _radioButtonToPathDictionary.Remove(radioButton);
                    radioButton.Dispose();
                }
            }
        }

        private void SaveButton_Clicked(object sender, EventArgs args)
        {
            _titleUpdateWindowData.Paths.Clear();
            foreach (string paths in _radioButtonToPathDictionary.Values)
            {
                _titleUpdateWindowData.Paths.Add(paths);
            }

            foreach (RadioButton radioButton in _noUpdateRadioButton.Group)
            {
                if (radioButton.Active)
                {
                    _titleUpdateWindowData.Selected = _radioButtonToPathDictionary.TryGetValue(radioButton, out string updatePath) ? updatePath : "";
                }
            }

            IJsonFormatterResolver resolver = CompositeResolver.Create(StandardResolver.AllowPrivateSnakeCase);

            string path = System.IO.Path.Combine(_virtualFileSystem.GetBasePath(), "games", _titleId, "updates.json");
            byte[] data = JsonSerializer.Serialize(_titleUpdateWindowData, resolver);

            File.WriteAllText(path, Encoding.UTF8.GetString(data, 0, data.Length).PrettyPrintJson());

            MainWindow.UpdateGameTable();
            Dispose();
        }

        private void CancelButton_Clicked(object sender, EventArgs args)
        {
            Dispose();
        }
    }
}