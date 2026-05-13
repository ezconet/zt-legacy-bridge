SELECT
    NR_CPF          AS Cpf,
    NM_MOT          AS Nome,
    DT_NASC         AS DataNascimento,
    NR_TELEFONE     AS Telefone,
    DS_LOGRADOURO   AS Logradouro,
    NR_ENDERECO     AS Numero,
    DS_COMPL        AS Complemento,
    DS_BAIRRO       AS Bairro,
    CD_IBGE         AS CidadeIbge,
    SG_UF           AS Uf,
    NR_CEP          AS Cep,
    NR_RNTRC        AS RntrcNumero,
    FL_RNTRC_ATIVO  AS RntrcAtivo,
    DT_RNTRC_VAL    AS RntrcValidade
FROM dbo.MOTORISTAS WITH (NOLOCK)
WHERE NR_CPF = @cpf;
