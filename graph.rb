require 'gtk2'
require 'cairo'
require 'utils'

module Cpg
	class Graph < Gtk::DrawingArea
		type_register

		attr_reader :sample_frequency, :unit_width, :yaxis, :data, :label, :color, :adjustment, :show_ruler

		def initialize(sample_frequency = 10, unit_width = 30, yaxis = [-1, 1])
			super()

			@adjustment = Gtk::Adjustment.new(0, 0, 0, 1, sample_frequency, 0)
			@changed_signal = @adjustment.signal_connect('value_changed') { |adj| redraw }
			@show_ruler = true

			self.sample_frequency = sample_frequency
			self.unit_width = unit_width
			self.yaxis = yaxis
		
			@data = []
			@backbuffer = nil
			@shift = 0
			@label = ''
			@color = [0, 0, 1]
			@sites = nil
		
			add_events(Gdk::Event::BUTTON1_MOTION_MASK | 
					   Gdk::Event::BUTTON_PRESS_MASK | 
					   Gdk::Event::BUTTON_RELEASE_MASK | 
					   Gdk::Event::KEY_PRESS_MASK | 
					   Gdk::Event::KEY_RELEASE_MASK |
					   Gdk::Event::POINTER_MOTION_MASK |
					   Gdk::Event::LEAVE_NOTIFY_MASK)
		
			double_buffered = true
		end
		
		def show_ruler=(val)
			@show_ruler = val
			queue_draw
		end
		
		def ruler
			@ruler.dup
		end
		
		def ruler=(r)
			@ruler = r
			queue_draw
		end
	
		def color=(val)
			@color = val
			redraw
		end
	
		def label=(val)
			@label = val
			redraw
		end
	
		def unit_width=(val)
			@unit_width = val
			
			ps = @sample_frequency * allocation.width / @unit_width.to_f
			@adjustment.page_size = ps unless ps.infinite?

			@adjustment.changed
		
			redraw
		end
		
		def auto_axis
			if @data && !@data.empty?
				range = [@data.min, @data.max]
			else
				range = [-3, 3]
			end
						
			dist = (range[1] - range[0]) / 2.0
			self.yaxis = [range[0] - dist * 0.2, range[1] + dist * 0.2]
		end
	
		def yaxis=(val)
			@yaxis = val
			
			if @yaxis[0] == 0.0 && @yaxis[1] == 0.0
				@yaxis[0] = -1
				@yaxis[1] = 1
			elsif @yaxis[0] == @yaxis[1]
				@yaxis[0] -= 0.2 * @yaxis[0]
				@yaxis[1] += 0.2 * @yaxis[1]
			end
			
			redraw
		end
	
		def sample_frequency=(val)
			@sample_frequency = val

			@adjustment.page_increment = val
			
			ps = @sample_frequency * allocation.width / @unit_width.to_f
			@adjustment.page_size = ps unless ps.infinite?
			@adjustment.changed
		
			redraw
		end
		
		def set_ticks(width, start)
			# width is the number of pixels per tick unit
			# start is the tick unit value from the left
		end
	
		def data=(val)
			@data = val.dup
			
			@data.collect! { |x| (x.to_f.nan? || x.to_f.infinite?) ? 0.0 : x.to_f }
		
			last = (@adjustment.value >= @adjustment.upper - @adjustment.page_size)
			@adjustment.upper = @data.length
			@adjustment.value = [@adjustment.lower, @adjustment.upper - @adjustment.page_size].max if last
		
			@adjustment.changed

			redraw
		end
	
		def scale
			return @unit_width / @sample_frequency.to_f, 
				   -(allocation.height - 3) / (@yaxis[1] - @yaxis[0]).to_f
		end
	
		def <<(sample)
			@data << ((sample.to_f.nan? || sample.to_f.infinite?) ? 0.0 : sample.to_f)
		
			@adjustment.upper = @data.length
			@adjustment.changed
		
			if @adjustment.value >= @adjustment.upper - @adjustment.page_size - 1
				@adjustment.signal_handler_block(@changed_signal)
				@adjustment.value = @adjustment.upper - @adjustment.page_size
				@adjustment.signal_handler_unblock(@changed_signal)
			
				redraw_one
			end
		end
	
		def samples
			(allocation.width / @unit_width.to_f) * @sample_frequency
		end
	
		def prepare(ctx)
			dx, dy = scale

			ctx.translate(0.5, (@yaxis[1] * -dy).round + 1.5)
		end
	
		def set_graph_line(ctx)
			ctx.set_source_rgb(@color[0], @color[1], @color[2])
			ctx.line_width = 2
		end
	
		def draw_xaxis(ctx, from)
			ctx.move_to(from, 0)
		
			ctx.set_source_rgb(0, 0, 0)
			ctx.line_width = 1
			ctx.line_to(allocation.width, 0)
			ctx.stroke
		end
	
		def data_offset
			@adjustment.value.to_i
		end
	
		# Redraw the whole graph
		def redraw
			# create new empty backbuffer
			return unless window
		
			@backbuffer = nil
			@recreate = true

			queue_draw
		end
		
		def recreate
			@recreate = false
			@backbuffer = create_buffer
		
			# number of samples fit in the window
			n = samples
		
			# determine offset in @data to draw from
			offset = data_offset
		
			ctx = @backbuffer.create_cairo_context
			prepare(ctx)

			# draw the xaxis
			draw_xaxis(ctx, 0)
		
			# set line color
			set_graph_line(ctx)
		
			# move to first point
			start = [n.floor - (@data.length - offset), 0].max
			dx, dy = scale
		
			ctx.move_to(start * dx, @data[offset] * dy) if @data[offset] != nil
		
			# get all the other points
			slice = @data[(offset + 1)..-1]

			(slice || []).each_with_index do |sample, idx|
				# draw a line this next sample
				ctx.line_to((start + idx + 1) * dx, sample * dy)
			end

			ctx.stroke
		end
	
		def redraw_one
			# simply shift the backbuffer and add sample
			return unless window
			return if @data.length <= 1

			# copy current backbuffer, but shifted one sample
			dx, dy = scale		
		
			buf = create_buffer
			ctx = buf.create_cairo_context
		
			# now shift it, but on the whole pixel, otherwise it will be messy
			ctx.set_source_pixmap(@backbuffer, -dx, 0)
			ctx.paint
		
			# draw the points we now need to draw, according to this new
			# shift of ours
			prepare(ctx)

			n = samples

			ctx.line_cap = Cairo::LINE_CAP_ROUND

			draw_xaxis(ctx, (n - 4) * dx)
		
			# set line color
			set_graph_line(ctx)

			if @data[-2] != nil
				ctx.move_to((n - 4) * dx, @data[-2] * dy)
			end
		
			ctx.line_to((n - 3) * dx, @data[-1] * dy)
			ctx.stroke
		
			@backbuffer = buf
			queue_draw
		end
	
		def create_buffer
			buf = Gdk::Pixmap.new(window, allocation.width, allocation.height, -1)
			ctx = buf.create_cairo_context
		
			# paint it with white
			ctx.set_source_rgb(1, 1, 1)
			ctx.paint
		
			buf
		end
	
		def clip(ct, area)
			ct.rectangle(area.x, area.y, area.width, area.height)
			ct.clip
		end
	
		def draw_yaxis(ct)
			cx = allocation.width - 0.5
		
			ct.line_width = 1
			ct.set_source_rgb(0, 0, 0)
			ct.move_to(cx, 0)
			ct.line_to(cx, allocation.height)
			ct.stroke
		
			ym = ((@yaxis[1] * 100.0).to_i / 100.0).to_s
			e = ct.text_extents(ym)
			ct.move_to(cx - e.width - 5, e.height + 2)
			ct.show_text(ym)
		
			ym = ((@yaxis[0] * 100.0).to_i / 100.0).to_s
			e = ct.text_extents(ym)
			ct.move_to(cx - e.width - 5, allocation.height)
			ct.show_text(ym)
		end
		
		def draw_ruler(ct)
			ct.set_source_rgb(0.5, 0.6, 1)
			ct.line_width = 1.5
			ct.move_to(@ruler[0] + 0.5, 0)
			ct.line_to(@ruler[0] + 0.5, allocation.height)
			ct.stroke
			
			# find y position at @ruler[0] in data points		
			offset = data_offset
			start = [samples.floor - (@data.length - offset), 0].max
			dx, dy = scale
			
			dp = (@ruler[0] / dx)
			
			return if dp.floor < start
			
			dpb = dp.floor.to_i
			dpe = dp.ceil.to_i
			
			if dpe == dpb
				factor = 1
			else
				factor = (dpe - dp) / (dpe - dpb).to_f
			end
			
			pos1 = dpb + offset - start
			pos2 = dpe + offset - start
			
			cy = (((@data[pos1] || 0) * factor) + ((@data[pos2] || 0) * (1 - factor)))
			
			# draw label first
			s = format('%.2f', cy)
			e = ct.text_extents(s)
			
			ct.rectangle(@ruler[0] + 3, 1, e.width + 4, e.height + 4)
			ct.set_source_rgba(1, 1, 1, 0.7)
			ct.fill
			
			ct.move_to(@ruler[0] + 5, 3 + e.height)
			ct.set_source_rgba(0, 0, 0, 0.8)
			ct.show_text(s)
			ct.stroke
			
			prepare(ct)
			ct.arc(@ruler[0], cy * dy, 5, 0, 2 * Math::PI)
			ct.set_source_rgba(0.6, 0.6, 1, 0.5)
			ct.fill_preserve
			
			ct.set_source_rgb(0.5, 0.6, 1)
			ct.stroke
		end
	
		def signal_do_expose_event(event)
			if !@backbuffer && @recreate
				recreate
			end
			
			# set the clip area for cairo and blit the offscreen to it
			return unless @backbuffer
		
			ct = window.create_cairo_context
			ct.save
		
			clip(ct, event.area)
			ct.set_source_pixmap(@backbuffer, 0, 0)
			ct.paint
		
			ct.restore
		
			# paint label over it
			if @label && !@label.empty?
				e = ct.text_extents(@label)
				ct.rectangle(1, 1, e.width + 2 + 1, e.height + 2 + 1)

				ct.set_source_rgba(1, 1, 1, 0.7)
				ct.fill
			
				ct.move_to(1, 1 + 1 + e.height)
				ct.set_source_rgba(0, 0, 0, 0.8)
				ct.show_text(@label)
			end
		
			# paint yaxis
			draw_yaxis(ct)
			
			if @show_ruler && @ruler
				draw_ruler(ct)
			end
		end
	
		def signal_do_configure_event(event)
			super

			# update page size
			@adjustment.page_size = @sample_frequency * allocation.width / @unit_width.to_f
			@adjustment.changed
		
			if @adjustment.value > @adjustment.upper - @adjustment.page_size
				# no need to redraw twice
				@adjustment.signal_handler_block(@changed_signal)
				@adjustment.value = [@adjustment.lower, @adjustment.upper - @adjustment.page_size].max
				@adjustment.signal_handler_unblock(@changed_signal)
			end
		
			# create new backbuffer
			redraw
		end
	
		def signal_do_scroll_event(event)
			super
		
			factor = 0.2

			if event.direction == Gdk::EventScroll::UP
				factor *= -1
			elsif event.direction == Gdk::EventScroll::DOWN
				factor *= 1
			else
				return
			end
			
			dist = (@yaxis[1] - @yaxis[0]) / 2.0
			
			ax = [0, 0]
			ax[0] = @yaxis[0] - factor * dist
			ax[1] = @yaxis[1] + factor * dist
		
			self.yaxis = ax
		end
		
		def signal_do_motion_notify_event(event)
			@ruler = [event.x, event.y]
			
			queue_draw
			false
		end
		
		def signal_do_leave_notify_event(event)
			@ruler = nil
			queue_draw
			false
		end
	end
end
