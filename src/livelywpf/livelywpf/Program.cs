using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf
{
    public class Program
    {
        [System.STAThreadAttribute()]
        public static void Main()
        {
            using (new rootuwp.App())
            {
                livelywpf.App app = new livelywpf.App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
