using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class WebhookTests
{
    [Theory]
    [InlineData("ftp://example.com/hook")]
    [InlineData("/relativo")]
    [InlineData("https://usuario:senha@example.com/hook")]
    [InlineData("")]
    public void Url_precisa_ser_http_absoluta_sem_credenciais(string url) =>
        Assert.Throws<DomainException>(() => new Webhook(Guid.NewGuid(), url));

    [Fact]
    public void Nasce_ativo_com_segredo_e_trocar_o_segredo_gera_outro()
    {
        var webhook = new Webhook(Guid.NewGuid(), "https://example.com/hook");
        var secret = webhook.Secret;

        Assert.True(webhook.Active);
        Assert.StartsWith("whsec_", secret);

        webhook.RotateSecret();
        Assert.NotEqual(secret, webhook.Secret);
    }

    [Fact]
    public void Falhas_reagendam_com_espera_dobrando_ate_desistir()
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), "task.created", "{}");
        var now = DateTime.UtcNow;

        delivery.Fail(500, "HTTP 500", now);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(now.AddSeconds(30), delivery.NextAttemptAt);

        delivery.Fail(null, "Timed out", now);
        Assert.Equal(now.AddMinutes(1), delivery.NextAttemptAt);

        for (var i = 2; i < WebhookDelivery.MaxAttempts; i++)
            delivery.Fail(503, "HTTP 503", now);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(WebhookDelivery.MaxAttempts, delivery.Attempts);

        // Reenvio manual: volta para a fila do zero.
        delivery.Redeliver(now);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void Erro_longo_e_cortado() =>
        Assert.Equal(WebhookDelivery.ErrorMaxLength, Failed(new string('x', 1000)).LastError!.Length);

    private static WebhookDelivery Failed(string error)
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), "ping", "{}");
        delivery.Fail(null, error, DateTime.UtcNow);

        return delivery;
    }
}
