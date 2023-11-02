using Fanex.Settlement.Converter.Services;

namespace Fanex.Library.StaticClass;

public static class XmlConvertServiceStatic
{
    private static XmlConvertService instance;

    public static XmlConvertService Instance
    {
        get
        {
            return instance ??= new XmlConvertServiceImp();
        }
    }
}