using OrcaFacil.Application.DocumentTemplates;
using OrcaFacil.Application.Files;
using OrcaFacil.Infrastructure.Files;

namespace OrcaFacil.UnitTests;
public sealed class ProfessionalDocumentSecurityTests
{
 [Theory] [InlineData("malware.exe")] [InlineData("script.js")] [InlineData("without_extension")]
 public void UploadPolicy_rejects_dangerous_extensions(string name) => Assert.Throws<ArgumentException>(()=>FileUploadPolicy.ValidateAndGetExtension(name,10));
 [Theory] [InlineData("../secret.pdf")] [InlineData("folder/file.pdf")]
 public void UploadPolicy_rejects_path_traversal(string name) => Assert.Throws<ArgumentException>(()=>FileUploadPolicy.ValidateAndGetExtension(name,10));
 [Fact] public async Task Private_storage_writes_hashed_real_content_below_account_boundary()
 { var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));try{var service=new LocalFileStorageService(root);var account=Guid.NewGuid();await using var input=new MemoryStream("real document"u8.ToArray());var saved=await service.SaveAsync(account,"receipt.pdf",input);Assert.Equal(64,saved.Sha256Hash.Length);Assert.StartsWith(account.ToString("N"),saved.RelativePath);await using var read=await service.OpenReadAsync(saved.RelativePath);Assert.Equal(input.Length,read.Length);}finally{if(Directory.Exists(root))Directory.Delete(root,true);} }
 [Fact] public void Template_validator_rejects_script_and_unknown_variable(){var errors=DocumentTemplateValidator.Validate("<script>alert(1)</script>{{segredo}}");Assert.Equal(2,errors.Count);}
 [Fact] public void Template_validator_accepts_commercial_variables()=>Assert.Empty(DocumentTemplateValidator.Validate("Olá {{cliente_nome}}, total {{proposta_total}}"));
}
