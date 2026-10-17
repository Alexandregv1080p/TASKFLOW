import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { dateGroup, formatDate, formatRelativeDay, isOverdue, projectKey } from './format'

describe('projectKey', () => {
  // Mesmos casos do app Android (FormatTest.kt): a sigla precisa ser igual nos dois clientes.
  test('sigla do projeto', () => {
    expect(projectKey('Website')).toBe('WEB')
    expect(projectKey('Mobile App')).toBe('MA')
    expect(projectKey('Portal do Cliente')).toBe('PDC')
    expect(projectKey('Revisão anual')).toBe('RA')
    expect(projectKey('Ação')).toBe('ACA')
    expect(projectKey('Backlog do produto (v2) extra')).toBe('BDP')
    expect(projectKey('  !!  ')).toBe('TF')
    expect(projectKey('')).toBe('TF')
  })
})

describe('formatDate', () => {
  test('formata sem passar por Date, sem deslocar o dia por fuso', () => {
    expect(formatDate('2026-10-05T00:00:00')).toBe('05/10/2026')
    expect(formatDate('2026-01-01')).toBe('01/01/2026')
  })
})

// daysAgo (usada por dateGroup, formatRelativeDay e isOverdue) depende de "hoje":
// fixamos o relógio para os testes serem determinísticos.
describe('com data de hoje fixa', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-06-15T12:00:00'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('dateGroup', () => {
    test('hoje', () => {
      expect(dateGroup('2026-06-15T09:00:00')).toBe('Hoje')
    })

    test('ontem', () => {
      expect(dateGroup('2026-06-14T09:00:00')).toBe('Ontem')
    })

    test('nesta semana (menos de 7 dias)', () => {
      expect(dateGroup('2026-06-10T09:00:00')).toBe('Nesta semana')
    })

    test('no ultimo mes (ate 30 dias)', () => {
      expect(dateGroup('2026-05-20T09:00:00')).toBe('No último mês')
    })

    test('ha mais de um mes', () => {
      expect(dateGroup('2026-04-01T09:00:00')).toBe('Há mais de um mês')
    })
  })

  describe('formatRelativeDay', () => {
    test('hoje', () => {
      expect(formatRelativeDay('2026-06-15T09:00:00')).toBe('hoje')
    })

    test('ontem', () => {
      expect(formatRelativeDay('2026-06-14T09:00:00')).toBe('ontem')
    })

    test('alguns dias atras', () => {
      expect(formatRelativeDay('2026-06-10T09:00:00')).toBe('há 5 dias')
    })

    test('em meses', () => {
      expect(formatRelativeDay('2026-04-15T09:00:00')).toBe('há 2 meses')
    })

    test('em anos', () => {
      expect(formatRelativeDay('2025-01-01T09:00:00')).toBe('ano passado')
    })
  })

  describe('isOverdue', () => {
    test('data passada e status diferente de Done e atrasada', () => {
      expect(isOverdue('2026-06-14T00:00:00', 'Todo')).toBe(true)
      expect(isOverdue('2026-06-14T00:00:00', 'InProgress')).toBe(true)
    })

    test('data passada mas status Done nao conta como atrasada', () => {
      expect(isOverdue('2026-06-14T00:00:00', 'Done')).toBe(false)
    })

    test('sem dueDate nunca e atrasada', () => {
      expect(isOverdue(null, 'Todo')).toBe(false)
    })

    test('data futura nao e atrasada', () => {
      expect(isOverdue('2026-06-20T00:00:00', 'Todo')).toBe(false)
    })
  })
})
