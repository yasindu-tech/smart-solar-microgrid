import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Battery, CalendarClock, Plus, Sparkles, Zap } from 'lucide-react'
import { Button } from '../../components/ui/button'
import { Input } from '../../components/ui/input'
import { Label } from '../../components/ui/label'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet'

interface DummySlot {
  slotId: string
  date: string
  startTime: string
  endTime: string
  totalCapacity: number
  availableCapacity: number
  energyOfferedKwh: number
}

function isoDateOffset(days: number): string {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

function formatSlotDate(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00`).toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
  })
}

const EMPTY_FORM = { date: isoDateOffset(0), startTime: '', endTime: '', totalCapacity: '', energyOfferedKwh: '' }

/**
 * Placeholder UI only - not wired to the real /api/nodes/{nodeId}/slots endpoints yet.
 * "Add slot" and "Generate for today" mutate local state so the screen is interactive.
 */
export default function SlotManagement() {
  const { nodeId } = useParams<{ nodeId: string }>()
  const [slots, setSlots] = useState<DummySlot[]>([])
  const [isDialogOpen, setDialogOpen] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)

  const nextSlotId = () => `SL-${String(slots.length + 1).padStart(4, '0')}`

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const totalCapacity = Number(form.totalCapacity) || 0

    setSlots((prev) => [
      ...prev,
      {
        slotId: nextSlotId(),
        date: form.date,
        startTime: form.startTime,
        endTime: form.endTime,
        totalCapacity,
        availableCapacity: totalCapacity,
        energyOfferedKwh: Number(form.energyOfferedKwh) || 0,
      },
    ])

    setForm(EMPTY_FORM)
    setDialogOpen(false)
  }

  const handleGenerateForToday = () => {
    setSlots((prev) => [
      ...prev,
      {
        slotId: nextSlotId(),
        date: isoDateOffset(0),
        startTime: '08:00',
        endTime: '10:00',
        totalCapacity: 4,
        availableCapacity: 4,
        energyOfferedKwh: 20,
      },
    ])
  }

  const updateAvailableCapacity = (slotId: string, value: number) => {
    setSlots((prev) =>
      prev.map((slot) => (slot.slotId === slotId ? { ...slot, availableCapacity: value } : slot)),
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        to="/backoffice/nodes"
        className="inline-flex w-fit items-center gap-1 text-sm text-sg-primary hover:underline"
      >
        <ArrowLeft size={14} />
        Back to nodes
      </Link>

      <div className="flex items-center justify-between">
        <div>
          <h2 className="sg-h3">Slots for {nodeId}</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Bookable windows for the next 7 days. (Placeholder screen — not yet wired to the API.)
          </p>
        </div>

        <div className="flex gap-2">
          <Button variant="outline" onClick={handleGenerateForToday}>
            <Sparkles size={16} className="mr-2" />
            Generate for today
          </Button>

          <Button onClick={() => setDialogOpen(true)}>
            <Plus size={16} className="mr-2" />
            Add slot
          </Button>
        </div>
      </div>

      <Sheet open={isDialogOpen} onOpenChange={setDialogOpen}>
        <SheetContent className="p-0">
          <form onSubmit={handleSubmit} className="flex h-full flex-col">
            <SheetHeader>
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                  <CalendarClock size={18} />
                </div>
                <div>
                  <SheetTitle>Add slot</SheetTitle>
                  <SheetDescription>New bookable window for {nodeId}.</SheetDescription>
                </div>
              </div>
            </SheetHeader>

            <SheetBody className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
                <Label htmlFor="date">Date</Label>
                <Input
                  id="date"
                  type="date"
                  value={form.date}
                  onChange={(e) => setForm((p) => ({ ...p, date: e.target.value }))}
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="startTime">Start</Label>
                  <Input
                    id="startTime"
                    type="time"
                    value={form.startTime}
                    onChange={(e) => setForm((p) => ({ ...p, startTime: e.target.value }))}
                    required
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="endTime">End</Label>
                  <Input
                    id="endTime"
                    type="time"
                    value={form.endTime}
                    onChange={(e) => setForm((p) => ({ ...p, endTime: e.target.value }))}
                    required
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="totalCapacity">Total capacity</Label>
                  <Input
                    id="totalCapacity"
                    type="number"
                    min="1"
                    value={form.totalCapacity}
                    onChange={(e) => setForm((p) => ({ ...p, totalCapacity: e.target.value }))}
                    required
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="energyOfferedKwh">Energy offered (kWh)</Label>
                  <Input
                    id="energyOfferedKwh"
                    type="number"
                    min="0"
                    step="any"
                    value={form.energyOfferedKwh}
                    onChange={(e) => setForm((p) => ({ ...p, energyOfferedKwh: e.target.value }))}
                    required
                  />
                </div>
              </div>
            </SheetBody>

            <SheetFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancel
              </Button>
              <Button type="submit">Create slot</Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>

      {slots.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-sg-border py-16 text-center">
          <CalendarClock size={28} className="text-sg-text-muted" />
          <p className="sg-h4">No slots yet</p>
          <p className="sg-body-sm text-sg-text-secondary">Bookable windows for this node will appear here.</p>
        </div>
      ) : (
        <div className="flex flex-col divide-y divide-sg-border overflow-hidden rounded-lg border border-sg-border bg-sg-surface">
          {slots
            .slice()
            .sort((a, b) => (a.date + a.startTime).localeCompare(b.date + b.startTime))
            .map((slot) => {
              const pct = slot.totalCapacity > 0 ? Math.round((slot.availableCapacity / slot.totalCapacity) * 100) : 0
              const isFull = slot.availableCapacity <= 0

              return (
                <div
                  key={slot.slotId}
                  className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between sm:gap-6"
                >
                  <div className="flex items-center gap-4 sm:w-48 sm:shrink-0">
                    <div className="flex h-11 w-11 shrink-0 flex-col items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                      <CalendarClock size={18} />
                    </div>
                    <div className="min-w-0">
                      <p className="sg-body font-semibold text-sg-text">{formatSlotDate(slot.date)}</p>
                      <p className="sg-caption font-mono">
                        {slot.startTime}–{slot.endTime}
                      </p>
                    </div>
                  </div>

                  <div className="flex-1 sm:max-w-xs">
                    <div className="mb-1.5 flex items-center justify-between">
                      <span className="sg-caption flex items-center gap-1">
                        <Battery size={12} />
                        Capacity
                      </span>
                      <span className="sg-caption font-medium text-sg-text-secondary">
                        {slot.availableCapacity} / {slot.totalCapacity}
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-sg-surface-muted">
                      <div
                        className={`h-full rounded-full transition-all ${isFull ? 'bg-sg-text-disabled' : 'bg-sg-primary'}`}
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  </div>

                  <div className="flex items-center gap-5 sm:shrink-0">
                    <div className="flex flex-col gap-1">
                      <Label htmlFor={`available-${slot.slotId}`} className="sg-caption">
                        Available
                      </Label>
                      <Input
                        id={`available-${slot.slotId}`}
                        type="number"
                        min={0}
                        max={slot.totalCapacity}
                        value={slot.availableCapacity}
                        onChange={(e) => updateAvailableCapacity(slot.slotId, Number(e.target.value))}
                        className="h-8 w-16 text-center"
                      />
                    </div>

                    <div className="flex items-center gap-1.5 text-sg-text-secondary">
                      <Zap size={14} />
                      <span className="sg-body-sm font-medium">{slot.energyOfferedKwh} kWh</span>
                    </div>

                    <span className={`sg-badge ${isFull ? 'sg-badge-neutral' : 'sg-badge-success'}`}>
                      <span className="sg-status-dot" />
                      {isFull ? 'Full' : 'Bookable'}
                    </span>
                  </div>
                </div>
              )
            })}
        </div>
      )}
    </div>
  )
}
