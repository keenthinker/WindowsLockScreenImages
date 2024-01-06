/// <summary>
/// Helper class holding the needed information when an instance of it is added to a ListBox component.
/// </summary>
public class ListBoxMetaItem
{
    public ListBoxMetaItem(FileInfo fileInfo)
    {
        this.Info = fileInfo;
    }

    public FileInfo? Info { get; set; }

    public String HumanReadableSize()
    {
        var size = this.Info.Length;

        const int unit = 1024;
        var mu = new List<string> { "B", "KB", "MB", "GB", "PT" };
        while (size > unit)
        {
            mu.RemoveAt(0);
            size /= unit;
        }
        return $"{size}{mu[0]}";
    }

    public override String ToString()
    {
        return (this.Info != null) ? this.Info.Name : "Error";
    }
}