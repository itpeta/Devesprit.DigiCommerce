﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Devesprit.Data.Enums;

namespace Devesprit.Data.Domain
{
    [Table("Tbl_UserLikes")]
    public partial class TblUserLikes : BaseEntity
    {
        public DateTime Date { get; set; }
        [Required]
        public int PostId { get; set; }
        public virtual TblPosts Post { get; set; }
        public PostType? PostType { get; set; }
        [Required]
        public string UserId { get; set; }
        public virtual TblUsers User { get; set; }
    }
}
