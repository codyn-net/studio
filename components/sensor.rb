require 'components/state'
require 'utils'

module Cpg::Components
	class Sensor < State
		def initialize
			super
			
			@parentdraw = SimulatedObject.instance_method(:draw).bind(self)
		end
	
		def make_patterns
			@patouter = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patouter.add_color_stop_rgb(0, 227 / 255.0, 172 / 255.0, 127 / 255.0)
			@patouter.add_color_stop_rgb(@height, 1, 1, 1)

			@patinner = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patinner.add_color_stop_rgb(0, 1, 217 / 255.0, 193 / 255.0)
			@patinner.add_color_stop_rgb(@height * 0.8, 1, 1, 1)
		end
		
		def draw_circle(ct, radius, allocation)
			ct.arc(allocation.width / 2.0, 
				   allocation.height / 2.0,
				   radius,
				   0,
				   2 * Math::PI)
		end
		
		def draw(ct)
			if allocation.height != @height
				@height = allocation.height
				make_patterns
			end
			
			ct.save
			uw = ct.line_width

			ct.set_source(@patouter)

			draw_circle(ct, allocation.width / 2.0, allocation)
			ct.fill
		
			ct.line_width = uw * 2
			ct.set_source_rgb(130 / 255.0, 80 / 255.0, 26 / 255.0)
			draw_circle(ct, allocation.width / 2.0 * 0.9 - uw * 2, allocation)
			ct.stroke_preserve
			ct.set_source(@patinner)
			ct.fill
			
			draw_display(ct)
			
			ct.restore
			
			@parentdraw.call(ct)
		end
	end
end
