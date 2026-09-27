using System;
using Xunit;
using Vocalis.Audio;

namespace Vocalis.Test;

public class LivelloAudioTests
{
    [Fact]
    public void CalcolaRms_ConBufferShortSilenzioso_RitornaZero()
    {
        // Arrange
        short[] buffer = new short[100]; // Tutti zeri di default

        // Act
        double risultato = LivelloAudio.CalcolaRMS(buffer);

        // Assert
        Assert.Equal(0.0, risultato, precision: 5);
    }

    [Fact]
    public void CalcolaRms_ConBufferByteSilenzioso_RitornaZero()
    {
        // Arrange
        byte[] buffer = new byte[200]; // Tutti zeri

        // Act
        double risultato = LivelloAudio.CalcolaRMS(buffer);

        // Assert
        Assert.Equal(0.0, risultato, precision: 5);
    }

    [Fact]
    public void CalcolaRms_ConVolumeMassimoShort_RitornaVicinoAUno()
    {
        // Arrange - Un’onda quadra al massimo volume possibile (alterna max positivo e max negativo)
        short[] buffer = new short[] { 32767, -32768, 32767, -32768 };

        // Act
        double risultato = LivelloAudio.CalcolaRMS(buffer);

        // Assert
        // Essendo al massimo del range di un short a 16-bit, il valore RMS è virtualmente 1.0
        Assert.True(risultato > 0.99 && risultato <= 1.0);
    }

    [Fact]
    public void CalcolaRms_ConVolumeMassimoByte_RitornaVicinoAUno()
    {
        // Arrange - Rappresentazione in byte di 32767 (0xFF, 0x7F) e -32768 (0x00, 0x80)
        byte[] buffer = new byte[] { 0xFF, 0x7F, 0x00, 0x80 };

        // Act
        double risultato = LivelloAudio.CalcolaRMS(buffer);

        // Assert
        Assert.True(risultato > 0.99 && risultato <= 1.0);
    }

    [Theory]
    [InlineData(null)]
    public void CalcolaRms_ConShortNullOVuoto_RitornaZeroSenzaCrash(short[]? bufferInvalido)
    {
        double risultatoNull = LivelloAudio.CalcolaRMS(bufferInvalido);
        double risultatoVuoto = LivelloAudio.CalcolaRMS(Array.Empty<short>());

        Assert.Equal(0.0, risultatoNull);
        Assert.Equal(0.0, risultatoVuoto);
    }

    [Fact]
    public void CalcolaRms_ConByteNullOIncompleto_RitornaZeroSenzaCrash()
    {
        // Act
        double risultatoNull = LivelloAudio.CalcolaRMS((byte[]?)null);
        double risultatoVuoto = LivelloAudio.CalcolaRMS(Array.Empty<byte>());
        double risultatoUnSoloByte = LivelloAudio.CalcolaRMS(new byte[] { 0x01 }); // 1 solo byte non fa un campione a 16-bit

        // Assert
        Assert.Equal(0.0, risultatoNull);
        Assert.Equal(0.0, risultatoVuoto);
        Assert.Equal(0.0, risultatoUnSoloByte);
    }
}
