using PnP.Framework.Modernization.Entities;
using S3WebApi.Interfaces;

namespace S3WebApi.Services;

public class ChinaTermMappingSwitcher : IChinaTermMappingSwitcher
{
    //private readonly ChinaTermStoreHelper _termStoreHelper;
    //private readonly TermMapping _termMapping;

    public ChinaTermMappingSwitcher() { }

    //public ChinaTermMappingSwitcher(
    //    ChinaTermStoreHelper termStoreHelper,
    //    TermMapping termMapping)
    //{
    //    _termStoreHelper = termStoreHelper;
    //    _termMapping = termMapping;
    //}

    public void Switch()
    {
        //_termMapping.termIDDetails = _termStoreHelper.GetAllTermMapping();
        //_termMapping.termValCodeDetails = _termStoreHelper.GetAllValCodeMapping();
    }
}
