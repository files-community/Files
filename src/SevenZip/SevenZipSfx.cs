namespace SevenZip
{
#if SFX
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    using SfxSettings = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Sfx module choice enumeration
    /// </summary>
    public enum SfxModule
    {
        /// <summary>
        /// Default module (leave this if unsure)
        /// </summary>
        Default,
        /// <summary>
        /// The simple sfx module by Igor Pavlov with no adjustable parameters
        /// </summary>
        Simple,
        /// <summary>
        /// The installer sfx module by Igor Pavlov
        /// </summary>
        Installer,
        /// <summary>
        /// The extended installer sfx module by Oleg Scherbakov 
        /// </summary>
        Extended,
        /// <summary>
        /// The custom sfx module. First you must specify the module file name.
        /// </summary>
        Custom
    }

    /// <summary>
    /// The class for making 7-zip based self-extracting archives.
    /// </summary>
    public class SevenZipSfx
    {
        private static Dictionary<SfxModule, List<string>> SfxSupportedModuleNames
        {
            get
            {
                var result = new Dictionary<SfxModule, List<string>>
                {
                    {SfxModule.Simple, new List<string>(2) {"7z.sfx", "7zCon.sfx"}},
                    {SfxModule.Installer, new List<string>(2) {"7zS.sfx", "7zSD.sfx"}}
                };

                if (Environment.Is64BitProcess)
                {
                    result.Add(SfxModule.Default, new List<string>(1) { "7zxSD_All_x64.sfx" });
                    result.Add(SfxModule.Extended, new List<string>(4) { "7zxSD_All_x64.sfx", "7zxSD_Deflate_x64", "7zxSD_LZMA_x64", "7zxSD_PPMd_x64" });
                }
                else
                {
                    result.Add(SfxModule.Default, new List<string>(1) { "7zxSD_All.sfx" });
                    result.Add(SfxModule.Extended, new List<string>(4) { "7zxSD_All.sfx", "7zxSD_Deflate", "7zxSD_LZMA", "7zxSD_PPMd" });
                }

                return result;
            }
        }

        private string _moduleFileName;
        private Dictionary<SfxModule, List<string>> _sfxCommands;

        /// <summary>
        /// Initializes a new instance of the SevenZipSfx class.
        /// </summary>
        public SevenZipSfx()
        {
            SfxModule = SfxModule.Default;
            CommonInit();
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipSfx class.
        /// </summary>
        /// <param name="module">The sfx module to use as a front-end.</param>
        public SevenZipSfx(SfxModule module)
        {
            if (module == SfxModule.Custom)
            {
                throw new ArgumentException("You must specify the custom module executable.", nameof(module));
            }

            SfxModule = module;
            CommonInit();
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipSfx class.
        /// </summary>
        /// <param name="moduleFileName"></param>
        public SevenZipSfx(string moduleFileName)
        {
            SfxModule = SfxModule.Custom;
            ModuleFileName = moduleFileName;
            CommonInit();
        }

        /// <summary>
        /// Gets the sfx module type.
        /// </summary>
        public SfxModule SfxModule { get; private set; }

        /// <summary>
        /// Gets or sets the custom sfx module file name
        /// </summary>
        public string ModuleFileName
        {
            get => _moduleFileName;

            set
            {
                if (!File.Exists(value))
                {
                    throw new ArgumentException("The specified file does not exist.");
                }

                _moduleFileName = value;
                SfxModule = SfxModule.Custom;
                var sfxName = Path.GetFileName(value);

                foreach (var mod in SfxSupportedModuleNames.Keys)
                {
                    if (SfxSupportedModuleNames[mod].Contains(sfxName))
                    {
                        SfxModule = mod;
                    }
                }
            }
        }

        private void CommonInit()
        {
            LoadCommandsFromResource("Configs");
        }

        private static string GetResourceString(string str)
        {
            return "SevenZip.sfx." + str;
        }

        /// <summary>
        /// Gets the sfx module enum by the list of supported modules
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static SfxModule GetModuleByName(string name)
        {
            if (name.IndexOf("7z.sfx", StringComparison.Ordinal) > -1)
            {
                return SfxModule.Simple;
            }
            if (name.IndexOf("7zS.sfx", StringComparison.Ordinal) > -1)
            {
                return SfxModule.Installer;
            }
            if (name.IndexOf("7zxSD_All.sfx", StringComparison.Ordinal) > -1)
            {
                return SfxModule.Extended;
            }
            throw new SevenZipSfxValidationException("The specified configuration is unsupported.");
        }

        /// <summary>
        /// Loads the commands for each supported sfx module configuration
        /// </summary>
        /// <param name="xmlDefinitions">The resource name for xml definitions</param>
        private void LoadCommandsFromResource(string xmlDefinitions)
        {
            using (var cfg = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                GetResourceString(xmlDefinitions + ".xml")))
            {
                if (cfg == null)
                {
                    throw new SevenZipSfxValidationException("The configuration \"" + xmlDefinitions +
                                                             "\" does not exist.");
                }
                using (var schm = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    GetResourceString(xmlDefinitions + ".xsd")))
                {
                    if (schm == null)
                    {
                        throw new SevenZipSfxValidationException("The configuration schema \"" + xmlDefinitions +
                                                                 "\" does not exist.");
                    }
                    var sc = new XmlSchemaSet();
                    using (var scr = XmlReader.Create(schm))
                    {
                        sc.Add(null, scr);
                        var settings = new XmlReaderSettings {ValidationType = ValidationType.Schema, Schemas = sc};
                        var validationErrors = "";
                        settings.ValidationEventHandler +=
                            ((s, t) =>
                            {
                                validationErrors += String.Format(CultureInfo.InvariantCulture, "[{0}]: {1}\n",
                                                                  t.Severity.ToString(), t.Message);
                            });
                        using (var rdr = XmlReader.Create(cfg, settings))
                        {
                            _sfxCommands = new Dictionary<SfxModule, List<string>>();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.ReadStartElement("sfxConfigs");
                            rdr.Read();
                            do
                            {
                                var mod = GetModuleByName(rdr["modules"]);
                                rdr.ReadStartElement("config");
                                rdr.Read();
                                if (rdr.Name == "id")
                                {
                                    var cmds = new List<string>();
                                    _sfxCommands.Add(mod, cmds);
                                    do
                                    {
                                        cmds.Add(rdr["command"]);
                                        rdr.Read();
                                        rdr.Read();
                                    } while (rdr.Name == "id");
                                    rdr.ReadEndElement();
                                    rdr.Read();
                                }
                                else
                                {
                                    _sfxCommands.Add(mod, null);
                                }
                            } while (rdr.Name == "config");
                        }
                        if (!string.IsNullOrEmpty(validationErrors))
                        {
                            throw new SevenZipSfxValidationException(
                                "\n" + validationErrors.Substring(0, validationErrors.Length - 1));
                        }
                        _sfxCommands.Add(SfxModule.Default, _sfxCommands[SfxModule.Extended]);
                    }
                }
            }
        }

        /// <summary>
        /// Validates the sfx scenario commands.
        /// </summary>
        /// <param name="settings">The sfx settings dictionary to validate.</param>
        private void ValidateSettings(SfxSettings settings)
        {
            if (SfxModule == SfxModule.Custom)
            {
                return;
            }

            var commands = _sfxCommands[SfxModule];
            
            if (commands == null)
            {
                return;
            }
            
            var invalidCommands = new List<string>();
            
            foreach (var command in settings.Keys)
            {
                if (!commands.Contains(command))
                {
                    invalidCommands.Add(command);
                }
            }
            
            if (invalidCommands.Count > 0)
            {
                var invalidText = new StringBuilder("\nInvalid commands:\n");
                
                foreach (var str in invalidCommands)
                {
                    invalidText.Append(str);
                }
                
                throw new SevenZipSfxValidationException(invalidText.ToString());
            }
        }

        /// <summary>
        /// Gets the stream containing the sfx settings.
        /// </summary>
        /// <param name="settings">The sfx settings dictionary.</param>
        /// <returns></returns>
        private static Stream GetSettingsStream(SfxSettings settings)
        {
            var ms = new MemoryStream();
            var buf = Encoding.UTF8.GetBytes(@";!@Install@!UTF-8!" + '\n');
            ms.Write(buf, 0, buf.Length);
            
            foreach (var command in settings.Keys)
            {
                buf =
                    Encoding.UTF8.GetBytes(String.Format(CultureInfo.InvariantCulture, "{0}=\"{1}\"\n", command,
                                                         settings[command]));
                ms.Write(buf, 0, buf.Length);
            }
           
            buf = Encoding.UTF8.GetBytes(@";!@InstallEnd@!");
            ms.Write(buf, 0, buf.Length);
            
            return ms;
        }

        private SfxSettings GetDefaultSettings()
        {
            switch (SfxModule)
            {
                default:
                    return null;
                case SfxModule.Installer:
                    var settings = new Dictionary<string, string> {{"Title", "7-Zip self-extracting archive"}};
                    return settings;
                case SfxModule.Default:
                case SfxModule.Extended:
                    settings = new Dictionary<string, string>
                               {
                                   {"GUIMode", "0"},
                                   {"InstallPath", "."},
                                   {"GUIFlags", "128+8"},
                                   {"ExtractPathTitle", "7-Zip self-extracting archive"},
                                   {"ExtractPathText", "Specify the path where to extract the files:"}
                               };
                    return settings;
            }
        }

        /// <summary>
        /// Writes the whole to the other one.
        /// </summary>
        /// <param name="src">The source stream to read from.</param>
        /// <param name="dest">The destination stream to write to.</param>
        private static void WriteStream(Stream src, Stream dest)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            src.Seek(0, SeekOrigin.Begin);
            var buf = new byte[32768];
            int bytesRead;
            
            while ((bytesRead = src.Read(buf, 0, buf.Length)) > 0)
            {
                dest.Write(buf, 0, bytesRead);
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archive">The archive stream.</param>
        /// <param name="sfxFileName">The name of the self-extracting executable.</param>
        public void MakeSfx(Stream archive, string sfxFileName)
        {
            using (Stream sfxStream = File.Create(sfxFileName))
            {
                MakeSfx(archive, GetDefaultSettings(), sfxStream);
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archive">The archive stream.</param>
        /// <param name="sfxStream">The stream to write the self-extracting executable to.</param>
        public void MakeSfx(Stream archive, Stream sfxStream)
        {
            MakeSfx(archive, GetDefaultSettings(), sfxStream);
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archive">The archive stream.</param>
        /// <param name="settings">The sfx settings.</param>
        /// <param name="sfxFileName">The name of the self-extracting executable.</param>
        public void MakeSfx(Stream archive, SfxSettings settings, string sfxFileName)
        {
            using (Stream sfxStream = File.Create(sfxFileName))
            {
                MakeSfx(archive, settings, sfxStream);
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archive">The archive stream.</param>
        /// <param name="settings">The sfx settings.</param>
        /// <param name="sfxStream">The stream to write the self-extracting executable to.</param>
        public void MakeSfx(Stream archive, SfxSettings settings, Stream sfxStream)
        {
            if (!sfxStream.CanWrite)
            {
                throw new ArgumentException("The specified output stream can not write.", "sfxStream");
            }

            ValidateSettings(settings);

            using (var sfx = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetResourceString(SfxSupportedModuleNames[SfxModule][0])))
            {
                WriteStream(sfx, sfxStream);
            }

            if (SfxModule == SfxModule.Custom || _sfxCommands[SfxModule] != null)
            {
                using (var set = GetSettingsStream(settings))
                {
                    WriteStream(set, sfxStream);
                }
            }

            WriteStream(archive, sfxStream);
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archiveFileName">The archive file name.</param>
        /// <param name="sfxFileName">The name of the self-extracting executable.</param>
        public void MakeSfx(string archiveFileName, string sfxFileName)
        {
            using (Stream sfxStream = File.Create(sfxFileName))
            {
                using (
                    Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    )
                {
                    MakeSfx(archive, GetDefaultSettings(), sfxStream);
                }
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archiveFileName">The archive file name.</param>
        /// <param name="sfxStream">The stream to write the self-extracting executable to.</param>
        public void MakeSfx(string archiveFileName, Stream sfxStream)
        {
            using (Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                )
            {
                MakeSfx(archive, GetDefaultSettings(), sfxStream);
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archiveFileName">The archive file name.</param>
        /// <param name="settings">The sfx settings.</param>
        /// <param name="sfxFileName">The name of the self-extracting executable.</param>
        public void MakeSfx(string archiveFileName, SfxSettings settings, string sfxFileName)
        {
            using (Stream sfxStream = File.Create(sfxFileName))
            {
                using (
                    Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    )
                {
                    MakeSfx(archive, settings, sfxStream);
                }
            }
        }

        /// <summary>
        /// Makes the self-extracting archive.
        /// </summary>
        /// <param name="archiveFileName">The archive file name.</param>
        /// <param name="settings">The sfx settings.</param>
        /// <param name="sfxStream">The stream to write the self-extracting executable to.</param>
        public void MakeSfx(string archiveFileName, SfxSettings settings, Stream sfxStream)
        {
            using (Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                )
            {
                MakeSfx(archive, settings, sfxStream);
            }
        }
    }
#endif
}