using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBlazors.App.Components
{
    public interface ITheme
    {
        string GetClass<TComponent>(TComponent component);
    }
    public class Theme : ITheme
    {
        public string Name { get; set; } = "blue";
        public string GetClass<TComponent>(TComponent component)
        {
            return component == null ? "" : component.GetType().Name;
        }

        public override string ToString()
        {
            return $"Theme[{Name}]";
        }

        public static Theme Default = new Theme();
    }
}
