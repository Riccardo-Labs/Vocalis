using Vocalis.Trascrizione;

namespace Vocalis.Test;

public class CatalogoModelliTests
{
    [Fact]
    public void NomeFile_CostruisceIlNomeDelFile()
    {
        Assert.Equal("ggml-medium.bin", CatalogoModelli.NomeFile("medium"));
    }

    [Theory]
    [InlineData("ggml-large-v3-turbo.bin", "large-v3-turbo")]
    [InlineData("ggml-medium.bin", "medium")]
    [InlineData("GGML-Medium.BIN", "Medium")]
    public void NomeDaFile_ConModello_RitornaIlNome(string nomeFile, string atteso)
    {
        Assert.Equal(atteso, CatalogoModelli.NomeDaFile(nomeFile));
    }

    [Theory]
    [InlineData("ggml-medium.bin.tmp")]
    [InlineData("settings.json")]
    [InlineData("ggml-.bin")]
    [InlineData("medium.bin")]
    public void NomeDaFile_ConFileNonModello_RitornaNull(string nomeFile)
    {
        Assert.Null(CatalogoModelli.NomeDaFile(nomeFile));
    }

    [Fact]
    public void Risolvi_ConModelloPresente_LoUsa()
    {
        string risultato = CatalogoModelli.Risolvi("medium", ["large-v3-turbo", "medium"]);

        Assert.Equal("medium", risultato);
    }

    [Fact]
    public void Risolvi_ConModelloAssente_RipiegaSulPredefinito()
    {
        string risultato = CatalogoModelli.Risolvi("medium", ["large-v3-turbo"]);

        Assert.Equal(CatalogoModelli.NomePredefinito, risultato);
    }

    [Fact]
    public void Risolvi_ConNomeNullo_RipiegaSulPredefinito()
    {
        string risultato = CatalogoModelli.Risolvi(null, ["medium"]);

        Assert.Equal(CatalogoModelli.NomePredefinito, risultato);
    }
}
