using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FlexiSqlTools.Core
{
    public class StoredProcedureConfig
    {
        public StoredProcedureName Name { get; set; }
        public bool EnableEncryption { get; set; }
        public bool UseDataBase { get; set; }
        public bool UseGo { get; set; }

        //private Collection<string> _ExcludeFieldsInParameter = GetDefault_ExcludeFieldsInParameter();
        //private Collection<string> _ExcludeFieldsInWhere = GetDefault_ExcludeFieldsInWhere();
        //private Collection<string> _ExcludeSort;

        //public Collection<string> ExcludeFieldsInParameter
        //{
        //    get
        //    {
        //        if (_ExcludeFieldsInParameter == null)
        //        {
        //            _ExcludeFieldsInParameter = new Collection<string>();
        //        }
        //        return _ExcludeFieldsInParameter;
        //    }
        //    set { _ExcludeFieldsInParameter = value; }
        //}
        //public Collection<string> ExcludeFieldsInWhere
        //{
        //    get
        //    {
        //        if (_ExcludeFieldsInWhere == null)
        //        {
        //            _ExcludeFieldsInWhere = new Collection<string>();
        //        }
        //        return _ExcludeFieldsInWhere;
        //    }
        //    set { _ExcludeFieldsInWhere = value; }
        //}
        //public Collection<string> ExcludeFieldsInSort
        //{
        //    get
        //    {
        //        if (_ExcludeSort == null)
        //        {
        //            _ExcludeSort = new Collection<string>();
        //        }
        //        return _ExcludeSort;
        //    }
        //    set { _ExcludeSort = value; }
        //}

        //[Obsolete]
        //private static Collection<string> GetDefault_ExcludeFieldsInParameter()
        //{
        //    Collection<string> col = new Collection<string>();
        //    col.Add("LastModificationTime");
        //    col.Add("LastModifiedBy");
        //    return col;
        //}
        //[Obsolete]
        //private static Collection<string> GetDefault_ExcludeFieldsInWhere()
        //{
        //    Collection<string> col = new Collection<string>();
        //    col.Add("LastModificationTime");
        //    col.Add("LastModifiedBy");
        //    return col;
        //}
    }
}