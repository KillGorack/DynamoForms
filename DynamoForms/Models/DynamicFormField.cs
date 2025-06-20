public class DynamicFormField
{
    // --- Table Meta Data (from INFORMATION_SCHEMA or similar) ---
    public string Name { get; set; }             // fld_column
    public string Type { get; set; }             // fld_type
    public string Length { get; set; }           // fld_length
    public string Precision { get; set; }        // fld_precision
    public bool IsUnique { get; set; }           // fld_unique
    public bool Required { get; set; }           // fld_required
    public bool IsIdentity { get; set; }         // NEW: for identity/auto-increment

    // --- Field Configuration (from fld table) ---
    public int ID { get; set; }                  // ID
    public int AppId { get; set; }               // fld_app
    public string Label { get; set; }            // fld_human
    public bool Enabled { get; set; }            // fld_enable
    public bool IsOption { get; set; }           // fld_opt
    public string IconSet { get; set; }          // fld_icon_set
    public string Regex { get; set; }            // fld_regex
    public string UnitOfMeasure { get; set; }    // fld_uom
    public string Placeholder { get; set; }      // fld_placeholder
    public bool IsUserId { get; set; }           // fld_usr_ID
    public bool IsLink { get; set; }             // fld_link
    public bool ShowInDetail { get; set; }       // fld_detail
    public bool ShowInForm { get; set; }         // fld_form
    public bool ShowInList { get; set; }         // fld_index
    public int? Order { get; set; }              // fld_order
    public bool IsTitle { get; set; }            // fld_title
    public bool IsPassword { get; set; }         // fld_pass
    public bool IsDouble { get; set; }           // fld_double
    public bool IsEncrypted { get; set; }        // fld_encrypt
    public bool IsTime { get; set; }             // fld_time
    public bool IsImage { get; set; }            // fld_image
    public bool IsJson { get; set; }             // fld_json
}