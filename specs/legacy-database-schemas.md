use [bd_sap];
table: fornecedor 

campo cpf/cnpj = cgc_forn

field;type;length
cod_forn;			int           ;4    
cod_zt_forn;			nvarchar      ;12   
cod_zt_forn_OLD;		nvarchar      ;12   
nm_tipo;			nvarchar      ;60   
nome_forn;			nvarchar      ;1000 
razao_forn;			nvarchar      ;1000 
endereco_forn;			nvarchar      ;100  
bairro_forn;			nvarchar      ;100  
cidade_forn;			nvarchar      ;100  
uf_forn;			nvarchar      ;4    
email_forn;			varchar       ;1000 
cgc_forn;			nvarchar      ;40   
ie_forn;			nvarchar      ;30   
contato_forn;			nvarchar      ;60   
cep_forn;			nvarchar      ;20   
tel1_forn;			nvarchar      ;40   
tel2_forn;			nvarchar      ;40   
fax_forn;			nvarchar      ;40   
dt_cnh;				smalldatetime ;4    
dt_licen;			smalldatetime ;4    
dt_mop;				smalldatetime ;4    
dt_aso;				smalldatetime ;4    
dt_taco;			smalldatetime ;4    
dt_lopac;			smalldatetime ;4    
dt_antt;			smalldatetime ;4    
dt_inspveic;			smalldatetime ;4    
status_forn;			nchar         ;20   
codUserSite;			int           ;4    
dtVinculaSite;			smalldatetime ;4    
stSistema;			int           ;4    
codFornMicrosiga;		int           ;4    
mop;				bit           ;1    
dtIniAtividades;		smalldatetime ;4    
PesoCubado;			float         ;8    
m3;				float         ;8    
veiculo;			varchar       ;100  
ano;				int           ;4    
placa;				varchar       ;100  
fl_habilitar_quimico;		int           ;4    
nm_forn_mdf;			varchar       ;200  
cgc_veiculo;			nvarchar      ;80   
dtValidadeAnvisa;		smalldatetime ;4    
alteradoPor;			int           ;4    
alteradoEm;			smalldatetime ;4    
fl_transportadora;		bit           ;1    
cod_cidade;			int           ;4    
fl_xml_nfe;			bit           ;1    
fl_pdf_nfe;			bit           ;1    
fl_xml_cte;			bit           ;1    
fl_pdf_cte;			bit           ;1    
fl_xml_mdfe;			bit           ;1    
fl_pdf_mdfe;			bit           ;1    
fl_zip;				bit           ;1    
latitude_forn;			varchar       ;50   
longitude_forn;			varchar       ;50   
endereco_padrao;		varchar       ;2000 
dt_consulta_endereco;		datetime      ;8    

Text
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

CREATE PROCEDURE [dbo].[STP_SalvaDocumentoMotorista]
(
	@cod_forn			INT,
	@rg					varchar(30)=NULL,
	@cpf				varchar(11)=NULL,
	@expedicao			smalldatetime=NULL,
	@cnhValidade		smalldatetime=NULL,
	@cnhCategoria		varchar(30)=NULL,
	@licenciamento		smalldatetime=NULL,
	@controlar			bit=NULL,
	@buonny				smalldatetime=NULL,
	@inspecao			smalldatetime=NULL,
	@antt				smalldatetime=NULL,
	@laudoRuido			smalldatetime=NULL,
	@aso				smalldatetime=NULL,
	@epi				bit=NULL,
	@mop				bit=NULL,
	@validadeMOP		smalldatetime=NULL,
	@cnh				VARCHAR(100) = NULL
)

AS
DELETE TB_DocumentoMotorista WHERE cod_forn=@cod_forn
INSERT INTO TB_DocumentoMotorista
(
	cod_forn,
	rg,
	cpf,
	expedicao,
	cnhValidade,
	cnhCategoria,
	licenciamento,
	controlar,
	buonny,
	inspecao,
	antt,
	laudoRuido,
	aso,
	epi,
	mop,
	validadeMOP	,
	cnh
)
VALUES
(
	@cod_forn,
	@rg,
	@cpf,
	@expedicao,
	@cnhValidade,
	@cnhCategoria,
	@licenciamento,
	@controlar,
	@buonny,
	@inspecao,
	@antt,
	@laudoRuido,
	@aso,
	@epi,
	@mop,
	@validadeMOP,
	@cnh	
)


Completion time: 2026-05-13T11:28:35.2129976-03:00


Text
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

CREATE PROCEDURE [dbo].[STP_SalvaDadosVeiculo]
(
	@cod_forn			INT,
	@idTipo				INT,
	@placa				VARCHAR(30),
	@ano				INT,
	@marca				VARCHAR(30),
	@modelo				VARCHAR(30),
	@cor				VARCHAR(30),
	@altura				float,
	@comprimento		float,
	@largura			float,
	@alturaChapeu		float,
	@comprimentoChapeu	float,
	@larguraChapeu		float,
	@m3					float,
	@pesoReal			float,
	@pesoCubado			Float,
	@tara				float=null,
	@renavam			VARCHAR(11)=null,
	@rntrc				VARCHAR(8)=null,
	@uf					VARCHAR(2)=null
)
AS

DELETE TB_DadosVeiculo WHERE cod_forn=@cod_forn

INSERT INTO TB_DadosVeiculo
(
	cod_forn,
	idTipo,
	placa,
	ano,
	marca,
	modelo,
	cor,
	altura,
	comprimento,
	largura,
	alturaChapeu,
	comprimentoChapeu,
	larguraChapeu,
	m3,
	pesoReal,
	pesoCubado,
	tara	,
	renavam,
	rntrc	,
	uf		
)
VALUES 
(
	@cod_forn,
	@idTipo,
	@placa,
	@ano,
	@marca,
	@modelo,
	@cor,
	@altura,
	@comprimento,
	@largura,
	@alturaChapeu,
	@comprimentoChapeu,
	@larguraChapeu,
	@m3,
	@pesoReal,
	@pesoCubado,
	@tara	,
	@renavam,
	@rntrc	,
	@uf		
)


Completion time: 2026-05-13T11:28:46.6641441-03:00
