using Lively.Common;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Factories
{
    public class AppThemeFactory : IAppThemeFactory
    {
        public ThemeModel CreateFromFile(string filePath, string name, string description)
        {
            var themeDir = Path.Combine(Constants.CommonPaths.ThemeDir, Path.GetRandomFileName());
            Directory.CreateDirectory(themeDir);
            var copyFile = Path.Combine(themeDir, Path.GetFileName(filePath));
            File.Copy(filePath, Path.Combine(themeDir, copyFile));
            var theme = new ThemeModel(file: copyFile,
                preview: copyFile,
                name: name,
                type: ThemeType.picture,
                description: description,
                contact: null,
                license: null,
                accentColor: null,
                tags: null) { IsEditable = true };
            JsonStorage<ThemeModel>.StoreData(Path.Combine(themeDir, "theme.json"),
                new ThemeModel(theme) { File = Path.GetFileName(theme.File), Preview = Path.GetFileName(theme.Preview) });
            return theme;
        }

        public ThemeModel CreateFromDirectory(string themeDir)
        {
            var metadata = Path.Combine(themeDir, "theme.json");
            if (!File.Exists(metadata))
            {
                throw new FileNotFoundException();
            }

            var theme = JsonStorage<ThemeModel>.LoadData(metadata);
            return new ThemeModel(theme) { File = Path.Combine(themeDir, theme.File), Preview = Path.Combine(themeDir, theme.Preview), IsEditable = true };
        }
    }
}
