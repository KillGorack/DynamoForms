namespace DynamoForms.Models
{
    public class FieldAttribute
    {
        public int ID { get; set; }
        public int AppId { get; set; } // Maps to fld_app
        public string HumanName { get; set; } // Maps to fld_human
        public string ColumnName { get; set; } // Maps to fld_column
        public bool Enabled { get; set; } // Maps to fld_enable
        public string Type { get; set; } // Maps to fld_type
        public string Length { get; set; } // Maps to fld_length
        public string Precision { get; set; } // Maps to fld_precision
        public bool Required { get; set; } // Maps to fld_required
        public bool Option { get; set; } // Maps to fld_opt
        public string IconSet { get; set; } // Maps to fld_icon_set
        public string Regex { get; set; } // Maps to fld_regex
        public string UnitOfMeasure { get; set; } // Maps to fld_uom
        public string Placeholder { get; set; } // Maps to fld_placeholder
        public bool UserId { get; set; } // Maps to fld_usr_ID
        public bool Link { get; set; } // Maps to fld_link
        public bool Index { get; set; } // Maps to fld_index
        public bool Detail { get; set; } // Maps to fld_detail
        public bool Form { get; set; } // Maps to fld_form
        public int? Order { get; set; } // Maps to fld_order
        public bool Title { get; set; } // Maps to fld_title
        public bool Password { get; set; } // Maps to fld_pass
        public bool Double { get; set; } // Maps to fld_double
        public bool Encrypt { get; set; } // Maps to fld_encrypt
        public bool Time { get; set; } // Maps to fld_time
        public bool Image { get; set; } // Maps to fld_image
        public bool Unique { get; set; } // Maps to fld_unique
        public bool Json { get; set; } // Maps to fld_json
    }
}