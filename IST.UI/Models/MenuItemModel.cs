using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IST.UI.Models;

public class MenuItemModel
{
    public string Label { get; set; } = string.Empty;
    public Func<Task>? OnClickAsync { get; set; }
}
