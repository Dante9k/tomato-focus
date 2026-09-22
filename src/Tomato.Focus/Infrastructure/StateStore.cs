using System;
using System.IO;
using System.Xml.Serialization;

namespace Tomato
{
    public sealed class StateStore
    {
        readonly string path;
        public StateStore(string path)
        {
            this.path = path;
        }

        public Preferences Read()
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    return (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException))
                    throw;
                return new Preferences();
            }
        }

        public bool Write(Preferences preferences)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new XmlSerializer(typeof(Preferences)).Serialize(stream, preferences);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
                return true;
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException))
                    throw;
                return false;
            }
        }
    }
}
