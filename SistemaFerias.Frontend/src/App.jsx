import { useState, useEffect, useRef } from 'react'
import './App.css'

const API = '/api/ferias'
const ANO_MINIMO = `${new Date().getFullYear()}-01-01`

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

  const [buscaNome, setBuscaNome] = useState('')
  const [sugestoes, setSugestoes] = useState([])
  const [usuarioSelecionado, setUsuarioSelecionado] = useState(null)
  const [mostrarSugestoes, setMostrarSugestoes] = useState(false)
  const [dataInicio, setDataInicio] = useState('')
  const [dataRetorno, setDataRetorno] = useState('')
  const timerRef = useRef(null)
  const autocompleteRef = useRef(null)

  const hoje = new Date().toLocaleDateString('pt-BR', {
    weekday: 'long', day: '2-digit', month: 'long', year: 'numeric'
  })

  useEffect(() => { carregarFerias() }, [])

  useEffect(() => {
    function handleClickFora(e) {
      if (autocompleteRef.current && !autocompleteRef.current.contains(e.target)) {
        setMostrarSugestoes(false)
      }
    }
    document.addEventListener('mousedown', handleClickFora)
    return () => document.removeEventListener('mousedown', handleClickFora)
  }, [])

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

  function onBuscaNomeChange(val) {
    setBuscaNome(val)
    setUsuarioSelecionado(null)
    setSugestoes([])
    clearTimeout(timerRef.current)
    if (val.length < 2) { setMostrarSugestoes(false); return }
    timerRef.current = setTimeout(() => buscarUsuarios(val), 400)
  }

  async function buscarUsuarios(nome) {
    try {
      const res = await fetch(`${API}/buscar-usuario?nome=${encodeURIComponent(nome)}`)
      const data = await res.json()
      setSugestoes(data)
      setMostrarSugestoes(data.length > 0)
    } catch {
      setSugestoes([])
    }
  }

  function selecionarUsuario(usuario) {
    setUsuarioSelecionado(usuario)
    setBuscaNome(usuario.nome)
    setSugestoes([])
    setMostrarSugestoes(false)
  }

  async function cadastrar() {
    if (!usuarioSelecionado || !dataInicio || !dataRetorno) {
      showAlert('form', 'Selecione um funcionário e preencha as datas.', 'error'); return
    }

    const anoAtual = new Date().getFullYear()
    if (new Date(dataInicio).getFullYear() < anoAtual || new Date(dataRetorno).getFullYear() < anoAtual) {
      showAlert('form', `As datas devem ser do ano ${anoAtual} em diante.`, 'error'); return
    }

    try {
      const res = await fetch(API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          nomeFuncionario: usuarioSelecionado.nome,
          loginAd: usuarioSelecionado.login,
          dataInicio,
          dataRetorno
        })
      })
      if (res.ok) {
        showAlert('form', 'Férias cadastradas com sucesso!', 'success')
        setBuscaNome(''); setUsuarioSelecionado(null)
        setDataInicio(''); setDataRetorno('')
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
              <div className="form-group full" ref={autocompleteRef} style={{position:'relative'}}>
                <label>Funcionário</label>
                <input
                  value={buscaNome}
                  onChange={e => onBuscaNomeChange(e.target.value)}
                  placeholder="Digite o nome do funcionário..."
                  className={usuarioSelecionado ? 'valid' : ''}
                  autoComplete="off"
                />
                {usuarioSelecionado && (
                  <span className="hint ok">✓ {usuarioSelecionado.nome} — login: {usuarioSelecionado.login}</span>
                )}
                {mostrarSugestoes && (
                  <ul className="autocomplete-list">
                    {sugestoes.map((u, i) => (
                      <li key={i} onClick={() => selecionarUsuario(u)}>
                        <strong>{u.nome}</strong>
                        <span>{u.login}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="form-group">
                <label>Data de início</label>
                <input type="date" value={dataInicio} min={ANO_MINIMO} onChange={e => setDataInicio(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Data de retorno</label>
                <input type="date" value={dataRetorno} min={dataInicio || ANO_MINIMO} onChange={e => setDataRetorno(e.target.value)} />
              </div>
              <div className="form-group full">
                <button className="btn btn-primary" onClick={cadastrar} disabled={!usuarioSelecionado}>
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