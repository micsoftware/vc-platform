using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;



namespace VirtoCommerce.Client.Model {

  /// <summary>
  /// 
  /// </summary>
  [DataContract]
  public class VirtoCommerceContentWebModelsPageFolder {
    
    /// <summary>
    /// Page folder name, by-default 'pages' and 'includes'
    /// </summary>
    /// <value>Page folder name, by-default 'pages' and 'includes'</value>
    [DataMember(Name="folderName", EmitDefaultValue=false)]
    public string FolderName { get; set; }

    
    /// <summary>
    /// Collection of page elements in this folder
    /// </summary>
    /// <value>Collection of page elements in this folder</value>
    [DataMember(Name="pages", EmitDefaultValue=false)]
    public List<VirtoCommerceContentWebModelsPage> Pages { get; set; }

    
    /// <summary>
    /// Collection of folders
    /// </summary>
    /// <value>Collection of folders</value>
    [DataMember(Name="folders", EmitDefaultValue=false)]
    public List<VirtoCommerceContentWebModelsPageFolder> Folders { get; set; }

    
    /// <summary>
    /// Gets or Sets SecurityScopes
    /// </summary>
    [DataMember(Name="securityScopes", EmitDefaultValue=false)]
    public List<string> SecurityScopes { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class VirtoCommerceContentWebModelsPageFolder {\n");
      
      sb.Append("  FolderName: ").Append(FolderName).Append("\n");
      
      sb.Append("  Pages: ").Append(Pages).Append("\n");
      
      sb.Append("  Folders: ").Append(Folders).Append("\n");
      
      sb.Append("  SecurityScopes: ").Append(SecurityScopes).Append("\n");
      
      sb.Append("}\n");
      return sb.ToString();
    }

    /// <summary>
    /// Get the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson() {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}


}
