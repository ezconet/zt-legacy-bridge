SELECT TOP 1
    v.placa                                                                            AS Placa,
    v.idTipo                                                                           AS TipoVeiculoId,
    t.Descricao                                                                        AS TipoVeiculoDescricao,
    v.renavam                                                                          AS Renavam,
    v.ano                                                                              AS AnoFabricacao,
    v.ano                                                                              AS AnoModelo,
    v.marca                                                                            AS Marca,
    v.modelo                                                                           AS Modelo,
    v.tara                                                                             AS Tara,
    v.pesoReal                                                                         AS CapacidadeKg,
    v.rntrc                                                                            AS Rntrc,
    REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ','')         AS PropDocumento,
    f.nome_forn                                                                        AS PropNome,
    CASE
        WHEN LEN(REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ','')) = 14
            THEN 'CNPJ'
        ELSE 'CPF'
    END                                                                                AS PropTipoDocumento
FROM dbo.TB_DadosVeiculo v WITH (NOLOCK)
LEFT JOIN dbo.fornecedor f WITH (NOLOCK) ON f.cod_forn = v.cod_forn
LEFT JOIN [DB_WMS].dbo.TB_REC_Veiculos t WITH (NOLOCK) ON t.idVeiculo = v.idTipo
WHERE
    v.placa = @placa
    OR REPLACE(REPLACE(v.placa, '-', ''), ' ', '') = @placa;
