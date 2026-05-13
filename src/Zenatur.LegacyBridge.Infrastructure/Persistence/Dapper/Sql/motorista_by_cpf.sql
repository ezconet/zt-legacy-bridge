SELECT TOP 1
    COALESCE(d.cpf,
             REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ',''))
                                        AS Cpf,
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
    v.rntrc                             AS RntrcNumero,
    CAST(NULL AS BIT)                   AS RntrcAtivo,
    f.dt_antt                           AS RntrcValidade
FROM dbo.fornecedor f WITH (NOLOCK)
LEFT JOIN dbo.TB_DocumentoMotorista d WITH (NOLOCK) ON d.cod_forn = f.cod_forn
LEFT JOIN dbo.TB_DadosVeiculo v WITH (NOLOCK) ON v.cod_forn = f.cod_forn
WHERE
    d.cpf = @cpf
    OR REPLACE(REPLACE(REPLACE(REPLACE(f.cgc_forn,'.',''),'-',''),'/',''),' ','') = @cpf;
