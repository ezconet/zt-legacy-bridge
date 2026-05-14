DECLARE @CodForn INT;

SELECT TOP 1 @CodForn = f.cod_forn
FROM dbo.fornecedor f WITH (NOLOCK)
LEFT JOIN dbo.TB_DocumentoMotorista d WITH (NOLOCK) ON d.cod_forn = f.cod_forn
WHERE d.cpf = @documento
   OR REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ','') = @documento;

SELECT
    @documento                                                              AS Documento,
    CASE WHEN LEN(@documento) = 14 THEN 'CNPJ' ELSE 'CPF' END                AS TipoDocumento,
    f.nome_forn                                                              AS Nome,
    m.dt_nascimento                                                          AS DataNascimento,
    f.tel1_forn                                                              AS Telefone,
    COALESCE(m.ds_logradouro, f.endereco_forn)                               AS Logradouro,
    m.ds_numero                                                              AS Numero,
    m.ds_complemento                                                         AS Complemento,
    COALESCE(m.ds_bairro, f.bairro_forn)                                     AS Bairro,
    c.nr_municipio                                                           AS CidadeIbge,
    COALESCE(m.uf, f.uf_forn)                                                AS Uf,
    COALESCE(m.ds_cep, f.cep_forn)                                           AS Cep,
    f.dt_antt                                                                AS AnttValidade
FROM dbo.fornecedor f WITH (NOLOCK)
LEFT JOIN [bd_fin_zenatur].dbo.tb_motorista m WITH (NOLOCK)
    ON REPLACE(REPLACE(REPLACE(REPLACE(m.ds_cpf,'.',''),'-',''),'/',''),' ','') = @documento
LEFT JOIN [bd_fin_zenatur].dbo.tb_cidade c WITH (NOLOCK)
    ON c.cod_cidade = COALESCE(m.cod_cidade, f.cod_cidade)
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
