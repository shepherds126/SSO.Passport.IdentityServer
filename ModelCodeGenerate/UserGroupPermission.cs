//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ModelCodeGenerate
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserGroupPermission
    {
        public int Id { get; set; }
        public bool HasPermission { get; set; }
        public int UserGroupId { get; set; }
        public int RoleId { get; set; }
    
        public virtual Role Role { get; set; }
        public virtual UserGroup UserGroup { get; set; }
    }
}
