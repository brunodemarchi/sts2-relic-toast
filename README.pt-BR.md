# Relic Toast

[English](README.md)

Relic Toast adiciona um pop-up pequeno e configurável sempre que você obtém uma relíquia em Slay the Spire 2.

![Exemplo do Relic Toast](docs/assets/relicexample.gif)

O pop-up usa o nome, a descrição, a raridade e a arte da relíquia direto do jogo, então ele acompanha o idioma selecionado e funciona com relíquias específicas de personagem.

## Recursos

- Mostra um aviso quando você obtém uma relíquia.
- Usa o idioma atual do jogo automaticamente.
- Exibe arte, nome, raridade e descrição da relíquia.
- Permite ajustar posição, escala, offsets, animação e duração.
- Organiza várias relíquias em fila, sem empilhar vários pop-ups ao mesmo tempo.
- Inclui botão de teste e seletor de relíquia de teste no menu de configurações.
- Apenas local.

## Requisitos

- Slay the Spire 2 `v0.105.0` ou mais recente.
- BaseLib `v3.1.2` ou mais recente.

## Instalação

### Steam Workshop

Inscreva-se na Steam Workshop: [Relic Toast](https://steamcommunity.com/sharedfiles/filedetails/?id=3747521513)

Confirme que o BaseLib também está instalado e ativado.

### Instalação Manual

1. Baixe o zip mais recente do `RelicToast` na página de Releases do GitHub.
2. Feche Slay the Spire 2.
3. Extraia o zip dentro da pasta `mods` do Slay the Spire 2.

A pasta instalada deve ficar assim:

```text
Slay the Spire 2/
  mods/
    RelicToast/
      RelicToast.dll
      RelicToast.json
```

Abra o jogo com BaseLib e Relic Toast ativados.

## Configurações

Abra o menu de configurações de mods do BaseLib e selecione Relic Toast.

Configurações disponíveis:

- `Enabled`
- `Position`
- `Scale`
- `Offset X`
- `Offset Y`
- `Animation In`
- `Animation Out`
- `Time On Screen`
- `In Duration`
- `Out Duration`
- `Queue Delay`
- `Test Relic`
- `Test Toast`

Posições:

- `TopLeft`
- `TopCenter`
- `TopRight`
- `BottomLeft`
- `BottomCenter`
- `BottomRight`

Animações:

- `None`
- `Fade`
- `SlideLeftRight`
- `SlideRightLeft`
- `SlideTopBottom`
- `SlideBottomTop`

## Compilar do Código-Fonte

Instale o .NET 9 SDK e compile:

```powershell
dotnet build RelicToast.sln -c Release
```

Por padrão, o projeto procura Slay the Spire 2 no local padrão de instalação da Steam:

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
```

Se o jogo estiver instalado em outro lugar:

```powershell
dotnet build RelicToast.sln -c Release -p:Sts2Path="D:\SteamLibrary\steamapps\common\Slay the Spire 2"
```

Os arquivos para instalar são gerados aqui:

```text
bin/Release/RelicToast.dll
bin/Release/RelicToast.json
```

## Solução de Problemas

Se o aviso não aparecer:

- Confirme que o BaseLib está instalado e ativado.
- Confirme que o Relic Toast está instalado em `mods/RelicToast/`.
- Reinicie o jogo depois de trocar o `RelicToast.dll`.
- Tente usar o botão `Test Toast` nas configurações do Relic Toast.
- Confira o arquivo de log:

```text
%APPDATA%\SlayTheSpire2\RelicToast.log
```

Se o Windows não deixar você substituir `RelicToast.dll`, provavelmente o jogo ainda está aberto. Feche Slay the Spire 2 primeiro.

## Observações

Slay the Spire 2 está em desenvolvimento ativo, e APIs de modding podem mudar. Se uma atualização do jogo ou do BaseLib quebrar o Relic Toast, abra uma issue com a versão do jogo, a versão do BaseLib e o arquivo de log se possível.

## Licença

Relic Toast é distribuído sob a [Licença MIT](LICENSE).

## Nota de Desenvolvimento

Relic Toast foi criado com assistência de IA. Eu guiei o design das funcionalidades, testei dentro do jogo, reportei bugs, escolhi comportamentos/configurações e iterei na experiência de uso. A implementação é um mod C# para STS2/BaseLib/Harmony gerado e refinado em colaboração com IA.
