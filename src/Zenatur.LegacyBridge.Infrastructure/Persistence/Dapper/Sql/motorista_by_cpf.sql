DECLARE @CodForn INT;

SELECT TOP 1 @CodForn = f.cod_forn
FROM dbo.fornecedor f WITH (NOLOCK)
LEFT JOIN dbo.TB_DocumentoMotorista d WITH (NOLOCK) ON d.cod_forn = f.cod_forn
WHERE d.cpf = @cpf
   OR REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ','') = @cpf;

SELECT
    @cpf                                AS Cpf,
    f.nome_forn                         AS Nome,
    CAST(NULL AS DATE)                  AS DataNascimento,
    f.tel1_forn                         AS Telefone,
    f.endereco_forn                     AS Logradouro,
    CAST(NULL AS NVARCHAR(20))          AS Numero,
    CAST(NULL AS NVARCHAR(100))         AS Complemento,
    f.bairro_forn                       AS Bairro,
    CAST(NULL AS NVARCHAR(7))           AS CidadeIbge,
    f.uf_forn                           AS Uf,
    f.cep_forn                          AS Cep,
    f.dt_antt                           AS AnttValidade
FROM dbo.fornecedor f WITH (NOLOCK)
WHERE f.cod_forn = @CodForn;

SELECT
    v.placa                             AS Placa,
    v.idTipo                            AS TipoVeiculoId,
    t.Descricao                         AS TipoVeiculoDescricao,
    v.renavam                           AS Renavam,
    v.ano                               AS AnoFabricacao,
    v.ano                               AS AnoModelo,
    v.marca                             AS Marca,
    v.modelo                            AS Modelo,
    v.tara                              AS Tara,
    v.pesoReal                          AS CapacidadeKg,
    v.rntrc                             AS Rntrc
FROM dbo.TB_DadosVeiculo v WITH (NOLOCK)
LEFT JOIN [DB_WMS].dbo.TB_REC_Veiculos t WITH (NOLOCK) ON t.idVeiculo = v.idTipo
WHERE v.cod_forn = @CodForn;
