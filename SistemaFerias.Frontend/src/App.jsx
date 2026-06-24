import { useState, useEffect, useRef } from 'react'
import './App.css'

const API = '/api/ferias'

function Badge({ status }) {
  const map = {
    1: { label: 'Pendente', cls: 'badge-pendente' },
    2: { label: 'Em Férias', cls: 'badge-emferias' },
    3: { label: 'Finalizado', cls: 'badge-finalizado' },
  }
  const { label, cls } = map[status] || { label: '—', cls: '' }
  return <span className={`badge ${cls}`}>{label}</span>
}

function fmt(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('pt-BR')
}

export default function App() {
  const [aba, setAba] = useState('lista')
  const [registros, setRegistros] = useState([])
  const [busca, setBusca] = useState('')
  const [filtroStatus, setFiltroStatus] = useState('')
  const [alertLista, setAlertLista] = useState(null)
  const [alertForm, setAlertForm] = useState(null)

  // form
  const [nome, setNome] = useState('')
  const [email, setEmail] = useState('')
  const [emailHint, setEmailHint] = useState(null)
  const [loginValido, setLoginValido] = useState(false)
  const [dataInicio, setDataInicio] = useState('')
  const [dataRetorno, setDataRetorno] = useState('')
  const timerRef = useRef(null)

  const hoje = new Date().toLocaleDateString('pt-BR', {
    weekday: 'long', day: '2-digit', month: 'long', year: 'numeric'
  })

  useEffect(() => { carregarFerias() }, [])

  async function carregarFerias() {
    try {
      const res = await fetch(API)
      const data = await res.json()
      setRegistros(data)
    } catch {
      showAlert('lista', 'Erro ao carregar registros. Verifique se a API está rodando.', 'error')
    }
  }

  function showAlert(alvo, msg, tipo) {
    const set = alvo === 'lista' ? setAlertLista : setAlertForm
    set({ msg, tipo })
    setTimeout(() => set(null), 4000)
  }

  const filtrados = registros.filter(f =>
    (!busca || f.nomeFuncionario.toLowerCase().includes(busca.toLowerCase()) ||
      f.loginAd.toLowerCase().includes(busca.toLowerCase())) &&
    (!filtroStatus || f.status == filtroStatus)
  )

  const statTotal = registros.length
  const statEmFerias = registros.filter(f => f.status === 2).length
  const statSemana = registros.filter(f => {
    const ret = new Date(f.dataRetorno)
    const agora = new Date()
    const semana = new Date(); semana.setDate(agora.getDate() + 7)
    return f.status === 2 && ret >= agora && ret <= semana
  }).length

  function onEmailChange(val) {
    setEmail(val)
    setLoginValido(false)
    setEmailHint(null)
    clearTimeout(timerRef.current)
    if (!val.includes('@')) return
    const login = val.split('@')[0]
    setEmailHint({ msg: 'Verificando usuário no AD...', tipo: 'verificando' })
    timerRef.current = setTimeout(() => validarLogin(login), 700)
  }

  async function validarLogin(login) {
    try {
      const res = await fetch(`${API}/validar-login/${login}`)
      if (res.ok) {
        setEmailHint({ msg: `✓ Usuário "${login}" encontrado no AD.`, tipo: 'ok' })
        setLoginValido(true)
      } else {
        setEmailHint({ msg: `✗ Usuário "${login}" não encontrado no AD.`, tipo: 'erro' })
        setLoginValido(false)
      }
    } catch {
      setEmailHint({ msg: 'Erro ao verificar no AD.', tipo: 'erro' })
    }
  }

  async function cadastrar() {
    if (!nome || !email || !dataInicio || !dataRetorno) {
      showAlert('form', 'Preencha todos os campos.', 'error'); return
    }
    if (!loginValido) {
      showAlert('form', 'Verifique o e-mail — usuário não encontrado no AD.', 'error'); return
    }
    const loginAd = email.split('@')[0]
    try {
      const res = await fetch(API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nomeFuncionario: nome, loginAd, dataInicio, dataRetorno })
      })
      if (res.ok) {
        showAlert('form', 'Férias cadastradas com sucesso!', 'success')
        setNome(''); setEmail(''); setDataInicio(''); setDataRetorno('')
        setEmailHint(null); setLoginValido(false)
        carregarFerias()
      } else {
        const msg = await res.text()
        showAlert('form', msg || 'Erro ao cadastrar.', 'error')
      }
    } catch {
      showAlert('form', 'Erro de conexão com a API.', 'error')
    }
  }

  async function reativar(id) {
    if (!confirm('Deseja reativar este funcionário antes do prazo?')) return
    try {
      const res = await fetch(`${API}/${id}/reativar`, { method: 'POST' })
      if (res.ok) {
        showAlert('lista', 'Funcionário reativado com sucesso!', 'success')
        carregarFerias()
      } else {
        const msg = await res.text()
        showAlert('lista', msg || 'Erro ao reativar.', 'error')
      }
    } catch {
      showAlert('lista', 'Erro de conexão com a API.', 'error')
    }
  }

  async function excluir(id) {
    if (!confirm('Deseja excluir este registro?')) return
    try {
      const res = await fetch(`${API}/${id}`, { method: 'DELETE' })
      if (res.ok) {
        showAlert('lista', 'Registro excluído.', 'success')
        carregarFerias()
      } else {
        const msg = await res.text()
        showAlert('lista', msg || 'Erro ao excluir.', 'error')
      }
    } catch {
      showAlert('lista', 'Erro de conexão com a API.', 'error')
    }
  }

  return (
    <>
      <header>
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2">
          <rect x="3" y="4" width="18" height="18" rx="2"/>
          <line x1="16" y1="2" x2="16" y2="6"/>
          <line x1="8" y1="2" x2="8" y2="6"/>
          <line x1="3" y1="10" x2="21" y2="10"/>
        </svg>
        <h1>Sistema de Férias</h1>
        <span className="data-header">{hoje}</span>
      </header>

      <div className="container">
        <div className="stats">
          <div className="stat"><div className="stat-label">Total cadastrado</div><div className="stat-value">{statTotal}</div></div>
          <div className="stat em-ferias"><div className="stat-label">Em férias agora</div><div className="stat-value">{statEmFerias}</div></div>
          <div className="stat semana"><div className="stat-label">Retornam esta semana</div><div className="stat-value">{statSemana}</div></div>
        </div>

        <div className="tabs">
          <button className={`tab ${aba === 'lista' ? 'active' : ''}`} onClick={() => setAba('lista')}>Todos os registros</button>
          <button className={`tab ${aba === 'novo' ? 'active' : ''}`} onClick={() => setAba('novo')}>+ Cadastrar férias</button>
        </div>

        {aba === 'lista' && (
          <div className="card">
            <div className="filter-row">
              <input value={busca} onChange={e => setBusca(e.target.value)} placeholder="Buscar por nome ou login..." />
              <select value={filtroStatus} onChange={e => setFiltroStatus(e.target.value)}>
                <option value="">Todos os status</option>
                <option value="1">Pendente</option>
                <option value="2">Em Férias</option>
                <option value="3">Finalizado</option>
              </select>
              <button className="btn btn-primary btn-sm" onClick={carregarFerias}>Atualizar</button>
            </div>
            {alertLista && <div className={`alert alert-${alertLista.tipo}`}>{alertLista.msg}</div>}
            <table>
              <thead>
                <tr>
                  <th>Funcionário</th>
                  <th>Login AD</th>
                  <th>Início</th>
                  <th>Retorno</th>
                  <th>Status</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {filtrados.length === 0 ? (
                  <tr><td colSpan="6" className="empty">Nenhum registro encontrado.</td></tr>
                ) : filtrados.map(f => (
                  <tr key={f.id}>
                    <td><strong>{f.nomeFuncionario}</strong></td>
                    <td className="login">{f.loginAd}</td>
                    <td>{fmt(f.dataInicio)}</td>
                    <td>{fmt(f.dataRetorno)}</td>
                    <td><Badge status={f.status} /></td>
                    <td>
                      <div className="actions">
                        {f.status === 2 && <button className="btn btn-success btn-sm" onClick={() => reativar(f.id)}>Reativar</button>}
                        {f.status !== 2 && <button className="btn btn-danger btn-sm" onClick={() => excluir(f.id)}>Excluir</button>}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {aba === 'novo' && (
          <div className="card">
            <h2>Cadastrar período de férias</h2>
            {alertForm && <div className={`alert alert-${alertForm.tipo}`}>{alertForm.msg}</div>}
            <div className="form-grid">
              <div className="form-group">
                <label>Nome do funcionário</label>
                <input value={nome} onChange={e => setNome(e.target.value)} placeholder="Ex: João da Silva" />
              </div>
              <div className="form-group">
                <label>E-mail corporativo</label>
                <input
                  type="email"
                  value={email}
                  onChange={e => onEmailChange(e.target.value)}
                  placeholder="Ex: joao.silva@stratura.com.br"
                  className={loginValido ? 'valid' : emailHint?.tipo === 'erro' ? 'invalid' : ''}
                />
                {emailHint && <span className={`hint ${emailHint.tipo}`}>{emailHint.msg}</span>}
              </div>
              <div className="form-group">
                <label>Data de início</label>
                <input type="date" value={dataInicio} onChange={e => setDataInicio(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Data de retorno</label>
                <input type="date" value={dataRetorno} onChange={e => setDataRetorno(e.target.value)} />
              </div>
              <div className="form-group full">
                <button className="btn btn-primary" onClick={cadastrar} disabled={!loginValido}>
                  Cadastrar férias
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </>
  )
}