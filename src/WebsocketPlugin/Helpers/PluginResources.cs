namespace Logitech.LogiActions.WebsocketPlugin
{
    using System;
    using System.IO;
    using System.Reflection;

    // A helper class for managing plugin resources.
    // Note that the resource files handled by this class must be embedded in the plugin assembly at compile time.
    // That is, the Build Action of the files must be "Embedded Resource" in the plugin project.

    internal static class PluginResources
    {
        private static Assembly? _assembly;

        public static void Init(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            _assembly = assembly;
        }

        private static Assembly Assembly => _assembly ?? throw new InvalidOperationException("Plugin resources have not been initialized.");

        // Retrieves the names of all the resource files in the specified folder.
        // The parameter `folderName` must be specified as a full path, for example, `Logitech.LogiActions.WebsocketPlugin.Resources`.
        // Returns the full names of the resource files, for example, `Logitech.LogiActions.WebsocketPlugin.Resources.Resource.txt`.
        public static String[] GetFilesInFolder(String folderName) => Assembly.GetFilesInFolder(folderName);

        // Finds the first resource file with the specified file name.
        // Returns the full name of the found resource file.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static String FindFile(String fileName) => Assembly.FindFileOrThrow(fileName);

        // Finds all the resource files that match the specified regular expression pattern.
        // Returns the full names of the found resource files.
        // Example:
        //     `PluginResources.FindFiles(@"\w+\.txt$")` returns all the resource files with the extension `.txt`.
        public static String[] FindFiles(String regexPattern) => Assembly.FindFiles(regexPattern);

        // Finds the first resource file with the specified file name, and returns the file as a stream.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static Stream GetStream(String resourceName) => Assembly.GetStream(FindFile(resourceName));

        // Reads content of the specified text file, and returns the file content as a string.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static String ReadTextFile(String resourceName) => Assembly.ReadTextFile(FindFile(resourceName));

        // Reads content of the specified binary file, and returns the file content as bytes.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static Byte[] ReadBinaryFile(String resourceName) => Assembly.ReadBinaryFile(FindFile(resourceName));

        // Reads content of the specified image file, and returns the file content as a bitmap image.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static BitmapImage ReadImage(String resourceName) => Assembly.ReadImage(FindFile(resourceName));

        // Extracts the specified resource file to the given file path in the file system.
        // Throws `FileNotFoundException` if the resource file is not found, or a system exception if the output file cannot be written.
        public static void ExtractFile(String resourceName, String filePathName)
            => Assembly.ExtractFile(FindFile(resourceName), filePathName);
    }
}
