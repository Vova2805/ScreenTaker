//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ScreenTaker
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserShare
    {
        public Nullable<int> PersonId { get; set; }
        public Nullable<int> ImageId { get; set; }
        public Nullable<int> FolderId { get; set; }
        public string Email { get; set; }
        public int Id { get; set; }
    
        public virtual Folder Folder { get; set; }
        public virtual Image Image { get; set; }
        public virtual Person Person { get; set; }
    }
}
